using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.Text;
using MiniC.Exceptions;
using MiniC.Generators;
using MiniC.Scopes;
using static MiniC.MiniCParser;

namespace MiniC
{
    public class SymbolTableSemanticListener : MiniCBaseListener
    {
        public ParseTreeProperty<Scope> Scopes { get; set; } = new ParseTreeProperty<Scope>();
        public ParseTreeProperty<SymbolType> Types { get; } = new ParseTreeProperty<SymbolType>();
        public ParseTreeProperty<SymbolType> Conversion { get; } = new ParseTreeProperty<SymbolType>();

        private GlobalScope global;
        private Scope currScope;

        private int currVarArraySize;
        private SymbolType arrayInitType;

        public override void EnterCompilationUnit([NotNull] CompilationUnitContext context)
        {
            global = new GlobalScope();
            Scopes.Put(context, global);
            currScope = global;
            SymbolType.GlobalScope = global;
        }

        public override void EnterStructDeclaration([NotNull] StructDeclarationContext context)
        {
            var structId = context.Identifier();
            string name = "struct" + structId.GetText();

            if (!SymbolType.CheckType(name))
            {
                StructSymbol struct_ = new StructSymbol(name, currScope);
                currScope.AddSymbol(struct_);
                Scopes.Put(context, struct_);
                currScope = struct_;
            }
            else
                throw new SemanticException($"Repeating declaration of struct at {structId.Symbol.Line}:{structId.Symbol.Column}!");
        }

        public override void ExitStructDeclaration([NotNull] StructDeclarationContext context)
        {
            currScope = currScope.Parent;
        }

        private bool isFunDefinition;

        public override void EnterFunctionDefinition([NotNull] FunctionDefinitionContext context)
        {
            isFunDefinition = true;
        }

        public override void EnterFunctionHeader([NotNull] FunctionHeaderContext context)
        {
            var functionId = context.Identifier();
            string name = functionId.GetText();

            if (!currScope.CheckSymbol(name))
            {
                string type = context.typeSpecifier().GetText();
                SymbolType symbolType = SymbolType.GetType(type);
                if (symbolType != null)
                {
                    FunctionSymbol function_ = new FunctionSymbol(name, symbolType, currScope);
                    currScope.AddSymbol(function_);
                    Scopes.Put(context, function_);
                    currScope = function_;
                }
                else
                    throw new SemanticException($"Unexisting function return type '{type}' at " +
                        $"{functionId.Symbol.Line}:{functionId.Symbol.Column}");
            }
            else
                throw new SemanticException($"Repeating function name '{name}' at {functionId.Symbol.Line}:{functionId.Symbol.Column}");
        }

        public override void ExitParameterDeclaration([NotNull] ParameterDeclarationContext context)
        {
            var paramId = context.Identifier();
            string name = paramId.GetText();

            if (!currScope.CheckSymbol(name))
            {
                string type = context.typeSpecifier().GetText();
                SymbolType symbolType = SymbolType.GetType(type);
                if (symbolType != null)
                {
                    if (symbolType.Name == "void")
                        throw new SemanticException($"Void parameter definition at {paramId.Symbol.Line}:{paramId.Symbol.Column}!");

                    string typeQualifier = context.typeQualifier()?.GetText();
                    bool isConst = typeQualifier != null && typeQualifier == "const";
                    symbolType.IsConst = isConst;

                    var isArray = context.LeftBracket();
                    if (isArray != null)
                    {
                        symbolType.IsArray = true;
                        currVarArraySize = 0;
                    }
                    else
                    {
                        symbolType.IsArray = false;
                        currVarArraySize = -1;
                    }

                    VarSymbol param_ = new VarSymbol(name, symbolType, currVarArraySize);
                    currScope.AddSymbol(param_);
                }
                else
                    throw new SemanticException($"Unexisting parameter type '{type}' at {paramId.Symbol.Line}:{paramId.Symbol.Column}");
            }
            else
                throw new SemanticException($"Repeating parameter name '{name}' at {paramId.Symbol.Line}:{paramId.Symbol.Column}");
        }

        public override void ExitFunctionHeader([NotNull] FunctionHeaderContext context)
        {
            // Чтобы следующий compoundStatement имел в родителе заголовок данной функции
            if (!isFunDefinition)
                currScope = currScope.Parent;
            else
                isFunDefinition = false;
        }

        public override void EnterCompoundStatement([NotNull] CompoundStatementContext context)
        {
            LocalScope local = new LocalScope(currScope);
            Scopes.Put(context, local);
            currScope = local;
        }

        public override void ExitCompoundStatement([NotNull] CompoundStatementContext context)
        {
            if (currScope.Parent is FunctionSymbol)
                currScope = currScope.Parent;

            currScope = currScope.Parent;
        }

        // int a[1];
        // float b;
        public override void EnterVarDeclaration([NotNull] VarDeclarationContext context)
        {
            var isArray = context.LeftBracket();
            if (isArray != null)
                currVarArraySize = int.Parse(context.IntegerConstant().GetText());
            else
                currVarArraySize = -1;
        }

        // int a = 1;
        // float b[] = { a, 2.0 };
        public override void EnterVarDefinition([NotNull] VarDefinitionContext context)
        {
            var isArray = context.LeftBracket();
            var initializer = context.initializer().GetText();
            if (isArray != null)
            {
                if (!initializer.Contains('{') && !initializer.Contains('}'))
                    throw new SemanticException($"Bad array initializing at {isArray.Symbol.Line}:{isArray.Symbol.Column}!");

                currVarArraySize = initializer.Split(',').Length;
            }
            else
            {
                if (initializer.Contains('{') || initializer.Contains('}'))
                {
                    var lBrace = context.initializer().LeftBrace().Symbol;
                    throw new SemanticException($"Bad variable initializing at {lBrace.Line}:{lBrace.Column}!");
                }

                currVarArraySize = -1;
            }
        }

        public override void ExitVarDefinition([NotNull] VarDefinitionContext context)
        {
            var rValueType = Types.Get(context.initializer());

            var lValueType = Types.Get(context.varHeader());
            if (IsStruct(lValueType.Name))
                throw new SemanticException($"Can't initialize structure at {context.Start.Line}:{context.Start.Column}!");

            if (!lValueType.IsFullEqual(rValueType) && IsStruct(rValueType.Name))
                throw new SemanticException($"Can't initialize with structure at {context.Start.Line}:{context.Start.Column}!");

            Types.Put(context, lValueType);

            MakeConversion(context, context.initializer());
        }

        // const int a
        // char b
        public override void ExitVarHeader([NotNull] VarHeaderContext context)
        {
            var varId = context.Identifier();
            string name = varId.GetText();

            if (!currScope.CheckSymbol(name))
            {
                string type = context.typeSpecifier().GetText();
                SymbolType symbolType = SymbolType.GetType(type);
                if (symbolType != null)
                {
                    if (symbolType.Name == "void")
                        throw new SemanticException($"Void variable or member definition at {varId.Symbol.Line}:{varId.Symbol.Column}!");

                    if (currScope is StructSymbol)
                        if (((StructSymbol) currScope).Name == symbolType.Name)
                            throw new SemanticException($"Recursive struct declaration at {varId.Symbol.Line}:{varId.Symbol.Column}");

                    if (currVarArraySize == -1)
                    {
                        symbolType.IsArray = false;
                    }
                    else
                    {
                        symbolType.IsArray = true;
                        arrayInitType = SymbolType.GetType(symbolType.Name);
                    }

                    string typeQualifier = context.typeQualifier()?.GetText();
                    bool isConst = typeQualifier != null && typeQualifier == "const";
                    symbolType.IsConst = isConst;

                    VarSymbol var_ = new VarSymbol(name, symbolType, currVarArraySize);
                    currScope.AddSymbol(var_);
                    Types.Put(context, symbolType);
                }
                else
                    throw new SemanticException($"Unexisting var type '{type}' at {varId.Symbol.Line}:{varId.Symbol.Column}!");
            }
            else
                throw new SemanticException($"Repeating variable or member name '{name}' at {varId.Symbol.Line}:{varId.Symbol.Column}!");
        }

        #region Initializer

        public override void ExitInitializer([NotNull] InitializerContext context)
        {
            // ternaryExpression
            if (context.ChildCount == 1)
            {
                var initType = Types.Get(context.ternaryExpression());
                if (!IsNumberType(initType))
                    throw new SemanticException($"Can't initialize with array or structure at " +
                        $"{context.Start.Line}:{context.Start.Column}!");

                Types.Put(context, initType);
            }
            // LeftBrace initializerList RightBrace
            else
            {
                // Копируем, чтобы элементы массива остались не массивами
                var initListType = SymbolType.GetType(Types.Get(context.initializerList()).Name);
                initListType.IsArray = true;

                Types.Put(context, initListType);
            }
        }

        public override void ExitInitializerList([NotNull] InitializerListContext context)
        {
            // initializer
            if (context.ChildCount == 1)
            {
                Types.Put(context, Types.Get(context.initializer()));
            }
            // initializerList Comma initializer
            else
            {
                Types.Put(context, arrayInitType);

                MakeConversion(context, context.initializer());
            }
        }

        #endregion

        #region Expressions

        public override void ExitExpression([NotNull] ExpressionContext context)
        {
            Types.Put(context, Types.Get(context.assignmentExpression()));
        }

        public override void ExitAssignmentExpression([NotNull] AssignmentExpressionContext context)
        {
            // ternaryExpression
            if (context.ChildCount == 1)
            {
                Types.Put(context, Types.Get(context.ternaryExpression()));
            }
            // lValueExpression assignmentOperator ternaryExpression
            else
            {
                // Запрет целиком присваивать массивы и структуры
                var rValueType = Types.Get(context.ternaryExpression());
                if (!IsNumberType(rValueType))
                    throw new SemanticException($"RValue can't be array or structure or void " +
                        $"at {context.Start.Line}:{context.Start.Column}!");

                var lValueType = Types.Get(context.lValueExpression());
                if (!IsNumberType(lValueType))
                    throw new SemanticException($"LValue can't be array or structure at {context.Start.Line}:{context.Start.Column}!");

                string operation = context.assignmentOperator().GetText();
                if (IsFloat(rValueType) && (operation == "<<=" || operation == ">>=" || operation == "%=" || operation == "&=" ||
                    operation == "^=" || operation == "|="))
                    throw new SemanticException($"Can't use '{operation}' with float value on the right " +
                        $"at {context.Start.Line}:{context.Start.Column}!");

                Types.Put(context, lValueType);

                MakeConversion(context, context.ternaryExpression());
            }
        }

        public override void ExitTernaryExpression([NotNull] TernaryExpressionContext context)
        {
            // logicalOrExpression
            if (context.ChildCount == 1)
            {
                Types.Put(context, Types.Get(context.logicalOrExpression()));
            }
            // logicalOrExpression Question ternaryExpression Colon ternaryExpression
            else
            {
                var logExpType = Types.Get(context.logicalOrExpression());
                if (!IsNumberType(logExpType))
                    throw new SemanticException($"Ternary condition can't be array or structure or void at " +
                        $"{context.Start.Line}:{context.Start.Column}!");

                var ternaryFirst = context.ternaryExpression(0);
                var ternarySecond = context.ternaryExpression(1);
                var ternaryFirstType = Types.Get(ternaryFirst);
                var ternarySecondType = Types.Get(ternarySecond);

                SymbolType resultType = ternaryFirstType;
                if (IsNumberType(ternaryFirstType) && IsNumberType(ternarySecondType))
                    resultType = SymbolType.GetBigger(ternaryFirstType, ternarySecondType);
                else
                {
                    if (!ternaryFirstType.IsFullEqual(ternarySecondType))
                        throw new SemanticException($"Ternary operation different return types at " +
                            $"{context.Start.Line}:{context.Start.Column}!");
                }

                Types.Put(context, resultType);

                MakeConversion(context, ternaryFirst);
                MakeConversion(context, ternarySecond);
            }
        }

        public override void ExitLogicalOrExpression([NotNull] LogicalOrExpressionContext context)
        {
            // logicalAndExpression
            if (context.ChildCount == 1)
            {
                Types.Put(context, Types.Get(context.logicalAndExpression()));
            }
            // logicalOrExpression OrOr logicalAndExpression
            else
            {
                var leftChild = context.logicalOrExpression();
                var leftChildType = Types.Get(leftChild);
                if (!IsNumberType(leftChildType))
                    throw new SemanticException($"Can't use '{context.children[1].GetText()}' on array or structure or void at " +
                        $"{context.Start.Line}:{context.Start.Column}!");

                var rightChild = context.logicalAndExpression();
                var rightChildType = Types.Get(rightChild);
                if (!IsNumberType(rightChildType))
                    throw new SemanticException($"Can't use '{context.children[1].GetText()}' on array or structure or void at " +
                        $"{context.Start.Line}:{context.Start.Column}!");

                Types.Put(context, SymbolType.GetType("int"));

                MakeConversion(context, leftChild);
                MakeConversion(context, rightChild);
            }
        }

        public override void ExitLogicalAndExpression([NotNull] LogicalAndExpressionContext context)
        {
            // inclusiveOrExpression
            if (context.ChildCount == 1)
            {
                Types.Put(context, Types.Get(context.inclusiveOrExpression()));
            }
            // logicalAndExpression AndAnd inclusiveOrExpression
            else
            {
                var leftChild = context.logicalAndExpression();
                var leftChildType = Types.Get(leftChild);
                if (!IsNumberType(leftChildType))
                    throw new SemanticException($"Can't use '{context.children[1].GetText()}' on array or structure or void at " +
                        $"{context.Start.Line}:{context.Start.Column}!");

                var rightChild = context.inclusiveOrExpression();
                var rightChildType = Types.Get(rightChild);
                if (!IsNumberType(rightChildType))
                    throw new SemanticException($"Can't use '{context.children[1].GetText()}' on array or structure or void at " +
                        $"{context.Start.Line}:{context.Start.Column}!");

                Types.Put(context, SymbolType.GetType("int"));

                MakeConversion(context, leftChild);
                MakeConversion(context, rightChild);
            }
        }

        public override void ExitInclusiveOrExpression([NotNull] InclusiveOrExpressionContext context)
        {
            // exclusiveOrExpression
            if (context.ChildCount == 1)
            {
                Types.Put(context, Types.Get(context.exclusiveOrExpression()));
            }
            // inclusiveOrExpression Or exclusiveOrExpression
            else
            {
                var leftChild = context.inclusiveOrExpression();
                var leftChildType = Types.Get(leftChild);
                if (!IsNumberType(leftChildType))
                    throw new SemanticException($"Can't use '{context.children[1].GetText()}' on array or structure or void at " +
                        $"{context.Start.Line}:{context.Start.Column}!");
                if (IsFloat(leftChildType))
                    throw new SemanticException($"Can't use '{context.children[1].GetText()}' with float value on the left " +
                        $"at {context.Start.Line}:{context.Start.Column}!");

                var rightChild = context.exclusiveOrExpression();
                var rightChildType = Types.Get(rightChild);
                if (!IsNumberType(rightChildType))
                    throw new SemanticException($"Can't use '{context.children[1].GetText()}' on array or structure or void at " +
                        $"{context.Start.Line}:{context.Start.Column}!");
                if (IsFloat(rightChildType))
                    throw new SemanticException($"Can't use '{context.children[1].GetText()}' with float value on the right " +
                        $"at {context.Start.Line}:{context.Start.Column}!");

                var biggerType = SymbolType.GetBigger(leftChildType, rightChildType);
                Types.Put(context, biggerType);

                MakeConversion(context, leftChild);
                MakeConversion(context, rightChild);
            }
        }

        public override void ExitExclusiveOrExpression([NotNull] ExclusiveOrExpressionContext context)
        {
            // andExpression
            if (context.ChildCount == 1)
            {
                Types.Put(context, Types.Get(context.andExpression()));
            }
            // exclusiveOrExpression Caret andExpression
            else
            {
                var leftChild = context.exclusiveOrExpression();
                var leftChildType = Types.Get(leftChild);
                if (!IsNumberType(leftChildType))
                    throw new SemanticException($"Can't use '{context.children[1].GetText()}' on array or structure or void at " +
                        $"{context.Start.Line}:{context.Start.Column}!");
                if (IsFloat(leftChildType))
                    throw new SemanticException($"Can't use '{context.children[1].GetText()}' with float value on the left " +
                        $"at {context.Start.Line}:{context.Start.Column}!");

                var rightChild = context.andExpression();
                var rightChildType = Types.Get(rightChild);
                if (!IsNumberType(rightChildType))
                    throw new SemanticException($"Can't use '{context.children[1].GetText()}' on array or structure or void at " +
                        $"{context.Start.Line}:{context.Start.Column}!");
                if (IsFloat(rightChildType))
                    throw new SemanticException($"Can't use '{context.children[1].GetText()}' with float value on the right " +
                        $"at {context.Start.Line}:{context.Start.Column}!");

                var biggerType = SymbolType.GetBigger(leftChildType, rightChildType);
                Types.Put(context, biggerType);

                MakeConversion(context, leftChild);
                MakeConversion(context, rightChild);
            }
        }

        public override void ExitAndExpression([NotNull] AndExpressionContext context)
        {
            // equalityExpression
            if (context.ChildCount == 1)
            {
                Types.Put(context, Types.Get(context.equalityExpression()));
            }
            // andExpression And equalityExpression
            else
            {
                var leftChild = context.andExpression();
                var leftChildType = Types.Get(leftChild);
                if (!IsNumberType(leftChildType))
                    throw new SemanticException($"Can't use '{context.children[1].GetText()}' on array or structure or void at " +
                        $"{context.Start.Line}:{context.Start.Column}!");
                if (IsFloat(leftChildType))
                    throw new SemanticException($"Can't use '{context.children[1].GetText()}' with float value on the left " +
                        $"at {context.Start.Line}:{context.Start.Column}!");

                var rightChild = context.equalityExpression();
                var rightChildType = Types.Get(rightChild);
                if (!IsNumberType(rightChildType))
                    throw new SemanticException($"Can't use '{context.children[1].GetText()}' on array or structure or void at " +
                        $"{context.Start.Line}:{context.Start.Column}!");
                if (IsFloat(rightChildType))
                    throw new SemanticException($"Can't use '{context.children[1].GetText()}' with float value on the right " +
                        $"at {context.Start.Line}:{context.Start.Column}!");

                var biggerType = SymbolType.GetBigger(leftChildType, rightChildType);
                Types.Put(context, biggerType);

                MakeConversion(context, leftChild);
                MakeConversion(context, rightChild);
            }
        }

        public override void ExitEqualityExpression([NotNull] EqualityExpressionContext context)
        {
            // relationalExpression
            if (context.ChildCount == 1)
            {
                Types.Put(context, Types.Get(context.relationalExpression()));
            }
            // equalityExpression eqOper relationalExpression
            else
            {
                var leftChild = context.equalityExpression();
                var leftChildType = Types.Get(leftChild);
                if (!IsNumberType(leftChildType))
                    throw new SemanticException($"Can't use '{context.children[1].GetText()}' on array or structure or void at " +
                        $"{context.Start.Line}:{context.Start.Column}!");

                var rightChild = context.relationalExpression();
                var rightChildType = Types.Get(rightChild);
                if (!IsNumberType(rightChildType))
                    throw new SemanticException($"Can't use '{context.children[1].GetText()}' on array or structure or void at " +
                        $"{context.Start.Line}:{context.Start.Column}!");

                Types.Put(context, SymbolType.GetType("int"));

                MakeConversion(context, leftChild);
                MakeConversion(context, rightChild);
            }
        }

        public override void ExitRelationalExpression([NotNull] RelationalExpressionContext context)
        {
            // shiftExpression
            if (context.ChildCount == 1)
            {
                Types.Put(context, Types.Get(context.shiftExpression()));
            }
            // relationalExpression relOper shiftExpression
            else
            {
                var leftChild = context.relationalExpression();
                var leftChildType = Types.Get(leftChild);
                if (!IsNumberType(leftChildType))
                    throw new SemanticException($"Can't use '{context.children[1].GetText()}' on array or structure or void at " +
                        $"{context.Start.Line}:{context.Start.Column}!");

                var rightChild = context.shiftExpression();
                var rightChildType = Types.Get(rightChild);
                if (!IsNumberType(rightChildType))
                    throw new SemanticException($"Can't use '{context.children[1].GetText()}' on array or structure or void at " +
                        $"{context.Start.Line}:{context.Start.Column}!");

                Types.Put(context, SymbolType.GetType("int"));

                MakeConversion(context, leftChild);
                MakeConversion(context, rightChild);
            }
        }

        public override void ExitShiftExpression([NotNull] ShiftExpressionContext context)
        {
            // additiveExpression
            if (context.ChildCount == 1)
            {
                Types.Put(context, Types.Get(context.additiveExpression()));
            }
            // shiftExpression shiftOper additiveExpression
            else
            {
                var leftChild = context.shiftExpression();
                var leftChildType = Types.Get(leftChild);
                if (!IsNumberType(leftChildType))
                    throw new SemanticException($"Can't use '{context.children[1].GetText()}' on array or structure or void at " +
                        $"{context.Start.Line}:{context.Start.Column}!");
                if (IsFloat(leftChildType))
                    throw new SemanticException($"Can't use '{context.children[1].GetText()}' with float value on the left " +
                        $"at {context.Start.Line}:{context.Start.Column}!");

                var rightChild = context.additiveExpression();
                var rightChildType = Types.Get(rightChild);
                if (!IsNumberType(rightChildType))
                    throw new SemanticException($"Can't use '{context.children[1].GetText()}' on array or structure or void at " +
                        $"{context.Start.Line}:{context.Start.Column}!");
                if (IsFloat(rightChildType))
                    throw new SemanticException($"Can't use '{context.children[1].GetText()}' with float value on the right " +
                        $"at {context.Start.Line}:{context.Start.Column}!");

                var biggerType = SymbolType.GetBigger(leftChildType, rightChildType);
                Types.Put(context, biggerType);

                MakeConversion(context, leftChild);
                MakeConversion(context, rightChild);
            }
        }

        public override void ExitAdditiveExpression([NotNull] AdditiveExpressionContext context)
        {
            // multiplicativeExpression
            if (context.ChildCount == 1)
            {
                Types.Put(context, Types.Get(context.multiplicativeExpression()));
            }
            // additiveExpression addOper multiplicativeExpression
            else
            {
                var leftChild = context.additiveExpression();
                var leftChildType = Types.Get(leftChild);
                if (!IsNumberType(leftChildType))
                    throw new SemanticException($"Can't use '{context.children[1].GetText()}' on array or structure or void at " +
                        $"{context.Start.Line}:{context.Start.Column}!");

                var rightChild = context.multiplicativeExpression();
                var rightChildType = Types.Get(rightChild);
                if (!IsNumberType(rightChildType))
                    throw new SemanticException($"Can't use '{context.children[1].GetText()}' on array or structure or void at " +
                        $"{context.Start.Line}:{context.Start.Column}!");

                var biggerType = SymbolType.GetBigger(leftChildType, rightChildType);
                Types.Put(context, biggerType);

                MakeConversion(context, leftChild);
                MakeConversion(context, rightChild);
            }
        }

        public override void ExitMultiplicativeExpression([NotNull] MultiplicativeExpressionContext context)
        {
            // unaryExpression
            if (context.ChildCount == 1)
            {
                Types.Put(context, Types.Get(context.unaryExpression()));
            }
            // multiplicativeExpression multOper unaryExpression
            else
            {
                var operation = context.children[1].GetText();

                var leftChild = context.multiplicativeExpression();
                var leftChildType = Types.Get(leftChild);
                if (!IsNumberType(leftChildType))
                    throw new SemanticException($"Can't use '{operation}' on array or structure or void at " +
                        $"{context.Start.Line}:{context.Start.Column}!");
                if (IsFloat(leftChildType) && operation == "%")
                    throw new SemanticException($"Can't use '{operation}' with float at {context.Start.Line}:{context.Start.Column}!");

                var rightChild = context.unaryExpression();
                var rightChildType = Types.Get(rightChild);
                if (!IsNumberType(rightChildType))
                    throw new SemanticException($"Can't use '{operation}' on array or structure or void at " +
                        $"{context.Start.Line}:{context.Start.Column}!");
                if (IsFloat(rightChildType) && operation == "%")
                    throw new SemanticException($"Can't use '{operation}' with float at {context.Start.Line}:{context.Start.Column}!");

                var biggerType = SymbolType.GetBigger(leftChildType, rightChildType);
                Types.Put(context, biggerType);

                MakeConversion(context, leftChild);
                MakeConversion(context, rightChild);
            }
        }

        public override void ExitUnaryExpression([NotNull] UnaryExpressionContext context)
        {
            // postfixExpression
            if (context.ChildCount == 1)
            {
                Types.Put(context, Types.Get(context.postfixExpression()));
            }
            // unaryOperator unaryExpression
            else
            {
                string operation = context.unaryOperator().GetText();

                var rightChildType = Types.Get(context.unaryExpression());
                if (!IsNumberType(rightChildType))
                    throw new SemanticException($"Can't use '{operation}' on array or structure or void at " +
                        $"{context.Start.Line}:{context.Start.Column}!");
                if (IsFloat(rightChildType) && operation == "~")
                    throw new SemanticException($"Can't use '{operation}' with float at {context.Start.Line}:{context.Start.Column}!");

                Types.Put(context, rightChildType);
            }
        }

        #region PostfixExpression

        public override void ExitPrimaryExp([NotNull] PrimaryExpContext context)
        {
            Types.Put(context, Types.Get(context.primaryExpression()));
        }

        public override void ExitArrayRead([NotNull] ArrayReadContext context)
        {
            // Слева от [] должен быть массив
            var leftChild = context.postfixExpression();
            var leftChildType = Types.Get(leftChild);
            if (!leftChildType.IsArray)
                throw new SemanticException($"Variable or member is not an array at {context.Start.Line}:{context.Start.Column}!");

            // Индекс должен быть целым числом
            var arrayIndexType = Types.Get(context.ternaryExpression());
            if (!IsNumberType(arrayIndexType))
                throw new SemanticException($"Array index can't be array or structure at {context.Start.Line}:{context.Start.Column}!");
            if (IsFloat(arrayIndexType))
                throw new SemanticException($"Array index can't be float value at {context.Start.Line}:{context.Start.Column}!");

            // Ложим в типы "разыменованый" массив
            Types.Put(context, SymbolType.GetType(leftChildType.Name));
        }

        public override void ExitStructRead([NotNull] StructReadContext context)
        {
            // Слева от . должна быть структура
            var leftChild = context.postfixExpression();
            var leftChildType = Types.Get(leftChild);
            var structSymbol = global.FindStruct(leftChildType);
            if (structSymbol == null)
                throw new SemanticException($"Try to get variable field of a non-struct type at " +
                    $"{context.Start.Line}:{context.Start.Column}!");

            // Если слева от . массив структур, то он обязательно сначала должен быть "разыменован"
            if (leftChildType.IsArray)
                throw new SemanticException($"Try to get field of array of structs at {context.Start.Line}:{context.Start.Column}!");

            // Проверям, что такое поле в структуре точно есть
            var memberId = context.Identifier();
            var memberSymbol = structSymbol.GetSymbol(memberId.GetText());
            if (memberSymbol != null)
                Types.Put(context, memberSymbol.Type);
            else
                throw new SemanticException($"Undefined struct member '{memberId.GetText()}' at " +
                    $"{memberId.Symbol.Line}:{memberId.Symbol.Column}!");
        }

        private Stack<FunctionSymbol> currFunctionCalls = new Stack<FunctionSymbol>();
        private Stack<int> currParameterIndex = new Stack<int>();

        public override void EnterFunctionCall([NotNull] FunctionCallContext context)
        {
            string functionName = context.Identifier().GetText();
            var functionSymbol = currScope.FindSymbol(functionName);
            if (functionSymbol == null || !(functionSymbol is FunctionSymbol))
                throw new SemanticException($"Can't find function '{functionName}' at {context.Start.Line}:{context.Start.Column}!");

            currFunctionCalls.Push((FunctionSymbol) functionSymbol);
            currParameterIndex.Push(0);
        }

        public override void ExitParameterList([NotNull] ParameterListContext context)
        {
            var currFunction = currFunctionCalls.Peek();
            int currParameter = currParameterIndex.Pop();

            if (currParameter >= currFunction.Table.Count)
                throw new SemanticException($"Too much parameters in the function call at {context.Start.Line}:{context.Start.Column}!");

            var param = context.ternaryExpression();
            var paramType = Types.Get(param);
            var awaitedParamType = currFunction.GetNumberedSymbol(currParameter).Type;

            if (!IsNumberType(paramType) || !IsNumberType(awaitedParamType))
            {
                if (!paramType.IsEqual(awaitedParamType))
                {
                    string awaitedArray = awaitedParamType.IsArray ? "[]" : "";
                    string array = paramType.IsArray ? "[]" : "";
                    throw new SemanticException($"Wrong {currParameter + 1} parameter type in the function call at " +
                        $"{context.Start.Line}:{context.Start.Column}, " +
                        $"awaited {awaitedParamType.Name}{awaitedArray} - recieved {paramType.Name}{array}!");
                }
            }

            if (paramType.IsConst && !awaitedParamType.IsConst)
                throw new SemanticException($"Parameter {currParameter + 1} is const when awaited not const in the function call" +
                    $"at {context.Start.Line}:{context.Start.Column}!");

            Types.Put(context, awaitedParamType);

            MakeConversion(context, param);

            currParameter++;
            currParameterIndex.Push(currParameter);
        }

        public override void ExitFunctionCall([NotNull] FunctionCallContext context)
        {
            var currFunction = currFunctionCalls.Pop();
            int currParameter = currParameterIndex.Pop();

            if (currParameter != currFunction.Table.Count)
                throw new SemanticException($"Wrong parameters count in the function call at {context.Start.Line}:{context.Start.Column}!");

            Types.Put(context, currFunction.Type);
        }

        #endregion

        #region PrimaryExpression

        public override void ExitVarRead([NotNull] VarReadContext context)
        {
            var varId = context.Identifier();
            string name = varId.GetText();

            var varSymbol = currScope.FindSymbol(name);
            if (varSymbol != null)
                Types.Put(context, varSymbol.Type);
            else
                throw new SemanticException($"Using an undefined variable '{name}' at {varId.Symbol.Line}:{varId.Symbol.Column}!");
        }

        public override void ExitConstRead([NotNull] ConstReadContext context)
        {
            Types.Put(context, Types.Get(context.constant()));
        }

        public override void ExitParens([NotNull] ParensContext context)
        {
            Types.Put(context, Types.Get(context.ternaryExpression()));
        }

        #endregion

        public override void ExitLValueExpression([NotNull] LValueExpressionContext context)
        {
            // Identifier
            if (context.ChildCount == 1)
            {
                var varId = context.Identifier();
                string name = varId.GetText();

                var varSymbol = currScope.FindSymbol(name);
                if (varSymbol != null)
                {
                    if (varSymbol.Type.IsConst)
                        throw new SemanticException($"Can't change const value at {varId.Symbol.Line}:{varId.Symbol.Column}!");

                    Types.Put(context, varSymbol.Type);
                }
                else
                    throw new SemanticException($"Using an undefined lvalue variable '{name}' at " +
                        $"{varId.Symbol.Line}:{varId.Symbol.Column}!");
            }
            // lValueExpression LeftBracket ternaryExpression RightBracket
            else if (context.ChildCount == 4)
            {
                // Слева от [] должен быть массив
                var leftChild = context.lValueExpression();
                var leftChildType = Types.Get(leftChild);
                if (!leftChildType.IsArray)
                    throw new SemanticException($"Variable or member is not an array at {context.Start.Line}:{context.Start.Column}!");

                // Индекс должен быть целым числом
                var arrayIndexType = Types.Get(context.ternaryExpression());
                if (!IsNumberType(arrayIndexType))
                    throw new SemanticException($"Array index can't be array or structure at {context.Start.Line}:{context.Start.Column}!");
                if (IsFloat(arrayIndexType))
                    throw new SemanticException($"Array index can't be float value at {context.Start.Line}:{context.Start.Column}!");

                // Ложим в типы "разыменованый" массив
                Types.Put(context, SymbolType.GetType(leftChildType.Name));
            }
            // lValueExpression Dot Identifier
            else
            {
                // Слева от . должна быть структура
                var leftChild = context.lValueExpression();
                var leftChildType = Types.Get(leftChild);
                var structSymbol = global.FindStruct(leftChildType);
                if (structSymbol == null)
                    throw new SemanticException($"Try to set variable field of a non-struct type at " +
                        $"{context.Start.Line}:{context.Start.Column}!");

                // Если слева от . массив структур, то он обязательно сначала должен быть "разыменован"
                if (leftChildType.IsArray)
                    throw new SemanticException($"Try to get field of array of structs at " +
                        $"{context.Start.Line}:{context.Start.Column}!");

                // Проверям, что такое поле в структуре точно есть
                var memberId = context.Identifier();
                var memberSymbol = structSymbol.GetSymbol(memberId.GetText());
                if (memberSymbol != null)
                {
                    if (memberSymbol.Type.IsConst)
                        throw new SemanticException($"Can't change const value at {memberId.Symbol.Line}:{memberId.Symbol.Column}!");

                    Types.Put(context, memberSymbol.Type);
                }
                else
                    throw new SemanticException($"Undefined struct member '{memberId.GetText()}' at " +
                        $"{memberId.Symbol.Line}:{memberId.Symbol.Column}!");
            }
        }

        #endregion

        #region Statements

        public override void ExitIfStatement([NotNull] IfStatementContext context)
        {
            var conditionType = Types.Get(context.ternaryExpression());
            if (!IsNumberType(conditionType))
                throw new SemanticException($"If condition can't be array or structure at " +
                    $"{context.Start.Line}:{context.Start.Column}!");
        }

        public override void ExitIterationStatement([NotNull] IterationStatementContext context)
        {
            var condition = context.ternaryExpression();
            if (condition != null)
            {
                var conditionType = Types.Get(context.ternaryExpression());
                if (!IsNumberType(conditionType))
                    throw new SemanticException($"Iteration condition can't be array or structure at " +
                                                $"{context.Start.Line}:{context.Start.Column}!");
            }
        }

        public override void ExitJumpStatement([NotNull] JumpStatementContext context)
        {
            // Return
            if (context.Return() != null)
            {
                var returnExp = context.ternaryExpression();

                var function = FindFunctionSymbolScope(currScope);
                if (function == null)
                    throw new SemanticException($"No function for return at {context.Start.Line}:{context.Start.Column}!");

                var functionType = function.Type;

                if (returnExp != null)
                {
                    var returnType = Types.Get(returnExp);
                    if (returnType.IsArray)
                        throw new SemanticException($"Can't return array at {context.Start.Line}:{context.Start.Column}!");

                    if (!IsNumberType(returnType) || !IsNumberType(functionType))
                        if (!returnType.IsFullEqual(functionType))
                            throw new SemanticException($"Return type don't match to the function one at " +
                                $"{context.Start.Line}:{context.Start.Column}!");
                }

                Types.Put(context, functionType);

                if (returnExp != null)
                    MakeConversion(context, returnExp);
            }
            // Break || Continue
            else
            {
                var cycleContext = FindClosestCycle(context);
                if (cycleContext == null)
                    throw new SemanticException($"'{context.children[0].GetText()}' outside of cycle block " +
                        $"at {context.Start.Line}:{context.Start.Column}!");
            }
        }

        #endregion

        public override void ExitConstant([NotNull] ConstantContext context)
        {
            if (context.IntegerConstant() != null)
                Types.Put(context, SymbolType.GetType("int"));
            else if (context.FloatingConstant() != null)
                Types.Put(context, SymbolType.GetType("float"));
            else if (context.CharacterConstant() != null)
                Types.Put(context, SymbolType.GetType("char"));
        }

        public override void ExitCompilationUnit([NotNull] CompilationUnitContext context)
        {
            Console.WriteLine("Global done");
            
            Console.WriteLine("Code after semantics and globals:");
            try
            {
                var a = new CompilationUnitCodeGenerator();
                var text = new AsmCodeWriter(Scopes, global, Conversion);
                text = a.GenerateCodeForContext(context, text);
                Console.WriteLine(text.AllCode);
                text.WriteToFile();
            }
            catch (CodeGenerationException e)
            {
                Console.WriteLine(e);
            }
        }

        #region Private Methods

        private bool IsStruct(string type)
        {
            return type.StartsWith("struct");
        }

        private bool IsNumberType(SymbolType type)
        {
            return !type.IsArray && !IsStruct(type.Name) && type.Name != "void";
        }

        private bool IsFloat(SymbolType type)
        {
            return type.Name == "float";
        }

        private FunctionSymbol FindFunctionSymbolScope(Scope currScope)
        {
            if (currScope is FunctionSymbol)
                return currScope as FunctionSymbol;
            else
            {
                if (currScope.Parent != null)
                    return FindFunctionSymbolScope(currScope.Parent);

                return null;
            }
        }

        private IterationStatementContext FindClosestCycle(RuleContext context)
        {
            if (context is IterationStatementContext)
                return context as IterationStatementContext;
            else
            {
                if (context.Parent is FunctionDefinitionContext)
                    return null;

                return FindClosestCycle(context.Parent);
            }
        }

        private void MakeConversion(RuleContext curr, RuleContext child)
        {
            var currType = Types.Get(curr);
            var childType = Types.Get(child);

            if (currType.Name != childType.Name)
            {
                Conversion.Put(child, currType);
                Console.WriteLine($"({currType.Name}) {((ParserRuleContext) child).GetText()} " +
                    $"at {((ParserRuleContext)child).Start.Line}:{((ParserRuleContext)child).Start.Column}");
            }
        }

        #endregion
    }
}
