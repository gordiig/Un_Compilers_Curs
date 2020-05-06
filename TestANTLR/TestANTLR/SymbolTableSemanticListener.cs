using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.Text;
using TestANTLR.Exceptions;
using TestANTLR.Scopes;
using static TestANTLR.MiniCParser;

namespace TestANTLR
{
    public class SymbolTableSemanticListener : MiniCBaseListener
    {
        public ParseTreeProperty<Scope> Scopes { get; set; } = new ParseTreeProperty<Scope>();
        public ParseTreeProperty<SymbolType> Types { get; set; } = new ParseTreeProperty<SymbolType>();

        private GlobalScope global;
        private Scope currScope;

        private int currVarArraySize;

        public override void EnterCompilationUnit([NotNull] CompilationUnitContext context)
        {
            global = new GlobalScope();
            Scopes.Put(context, global);
            currScope = global;
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
                    throw new SemanticException($"Unexisting function return type '{type}' at {functionId.Symbol.Line}:{functionId.Symbol.Column}");
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
                    string typeQualifier = context.typeQualifier()?.GetText();
                    bool isConst = typeQualifier != null && typeQualifier == "const";

                    var isArray = context.LeftBracket();
                    if (isArray != null)
                        currVarArraySize =  0;
                    else
                        currVarArraySize = -1;

                    VarSymbol param_ = new VarSymbol(name, symbolType, isConst, currVarArraySize);
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
            currScope = currScope.Parent;
        }

        public override void EnterCompoundStatement([NotNull] CompoundStatementContext context)
        {
            LocalScope local = new LocalScope(currScope);
            Scopes.Put(context, local);
            currScope = local;
        }

        public override void ExitCompoundStatement([NotNull] CompoundStatementContext context)
        {
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

                currVarArraySize = initializer.Split().Length;
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
                    if (currScope is StructSymbol)
                        if (((StructSymbol) currScope).Name == symbolType.TypeName())
                            throw new SemanticException($"Recursive struct declaration at {varId.Symbol.Line}:{varId.Symbol.Column}");

                    string typeQualifier = context.typeQualifier()?.GetText();
                    bool isConst = typeQualifier != null && typeQualifier == "const";
                    VarSymbol var_ = new VarSymbol(name, symbolType, isConst, currVarArraySize);
                    currScope.AddSymbol(var_);
                }
                else
                    throw new SemanticException($"Unexisting var type '{type}' at {varId.Symbol.Line}:{varId.Symbol.Column}!");
            }
            else
                throw new SemanticException($"Repeating variable name '{name}' at {varId.Symbol.Line}:{varId.Symbol.Column}!");
        }

        ///// PostfixExpression

        public override void ExitPrimaryExp([NotNull] PrimaryExpContext context)
        {
            Types.Put(context, Types.Get(context.children[0]));
        }

        public override void ExitArrayRead([NotNull] ArrayReadContext context)
        {
            
        }

        public override void ExitStructRead([NotNull] StructReadContext context)
        {
            var leftChild = context.children[0];
            var leftChildType = Types.Get(leftChild);
            if (leftChildType == null)
                throw new SemanticException($"Bad struct field construction at {context.Start.Line}!");

            var structSymbol = global.FindStruct(leftChildType);
            if (structSymbol == null)
                throw new SemanticException($"Try to get variable field of a non-struct type at line {context.Start.Line}!");

            var memberId = context.Identifier();
            var memberSymbol = structSymbol.GetSymbol(memberId.GetText());
            if (memberSymbol != null)
                Types.Put(context, memberSymbol.Type);
            else
                throw new SemanticException($"Undefined struct member '{memberSymbol.Name}' at {memberId.Symbol.Line}:{memberId.Symbol.Column}!");
        }

        ///// PrimaryExpression

        public override void EnterVarRead([NotNull] VarReadContext context)
        {
            var varId = context.Identifier();
            string name = varId.GetText();

            var varSymbol = currScope.FindSymbol(name);
            if (varSymbol != null)
                Types.Put(context, varSymbol.Type);
            else
                throw new SemanticException($"Using an undefined variable '{name}' at {varId.Symbol.Line}:{varId.Symbol.Column}!");
        }

        public override void ExitCompilationUnit([NotNull] CompilationUnitContext context)
        {
            Console.WriteLine("Global done");
        }
    }
}
