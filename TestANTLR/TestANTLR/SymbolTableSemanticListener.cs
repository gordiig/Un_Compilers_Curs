using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.Text;
using TestANTLR.Exceptions;
using TestANTLR.Generators;
using TestANTLR.Scopes;

namespace TestANTLR
{
    public class SymbolTableSemanticListener : MiniCBaseListener
    {
        public ParseTreeProperty<Scope> Scopes { get; set; } = new ParseTreeProperty<Scope>();

        private GlobalScope global;
        private Scope currScope;

        private int currVarArraySize;

        public override void EnterCompilationUnit([NotNull] MiniCParser.CompilationUnitContext context)
        {
            Console.WriteLine("Code before semantics and globals:");
            var a = new CompilationUnitCodeGenerator();
            var text = new AsmCodeWriter();
            text = a.GenerateCodeForContext(context, text);
            Console.WriteLine(text.AllCode);
            
            global = new GlobalScope();
            Scopes.Put(context, global);
            currScope = global;
        }

        public override void EnterStructDeclaration([NotNull] MiniCParser.StructDeclarationContext context)
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

        public override void ExitStructDeclaration([NotNull] MiniCParser.StructDeclarationContext context)
        {
            currScope = currScope.Parent;
        }

        public override void EnterFunctionHeader([NotNull] MiniCParser.FunctionHeaderContext context)
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
                    throw new SemanticException($"Unexisting function return type at {functionId.Symbol.Line}:{functionId.Symbol.Column}");
            }
            else
                throw new SemanticException($"Repeating function name at {functionId.Symbol.Line}:{functionId.Symbol.Column}");
        }

        public override void ExitParameterDeclaration([NotNull] MiniCParser.ParameterDeclarationContext context)
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
                    throw new SemanticException($"Unexisting parameter type at {paramId.Symbol.Line}:{paramId.Symbol.Column}");
            }
            else
                throw new SemanticException($"Repeating parameter name at {paramId.Symbol.Line}:{paramId.Symbol.Column}");
        }

        public override void ExitFunctionHeader([NotNull] MiniCParser.FunctionHeaderContext context)
        {
            currScope = currScope.Parent;
        }

        public override void EnterCompoundStatement([NotNull] MiniCParser.CompoundStatementContext context)
        {
            LocalScope local = new LocalScope(currScope);
            Scopes.Put(context, local);
            currScope = local;
        }

        public override void ExitCompoundStatement([NotNull] MiniCParser.CompoundStatementContext context)
        {
            currScope = currScope.Parent;
        }

        // int a[1];
        // float b;
        public override void EnterVarDeclaration([NotNull] MiniCParser.VarDeclarationContext context)
        {
            var isArray = context.LeftBracket();
            if (isArray != null)
                currVarArraySize = int.Parse(context.IntegerConstant().GetText());
            else
                currVarArraySize = -1;
        }

        // int a = 1;
        // float b[] = { a, 2.0 };
        public override void EnterVarDefinition([NotNull] MiniCParser.VarDefinitionContext context)
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
        public override void ExitVarHeader([NotNull] MiniCParser.VarHeaderContext context)
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
                    throw new SemanticException($"Unexisting var type at {varId.Symbol.Line}:{varId.Symbol.Column}");
            }
            else
                throw new SemanticException($"Repeating variable name at {varId.Symbol.Line}:{varId.Symbol.Column}");
        }

        public override void ExitCompilationUnit([NotNull] MiniCParser.CompilationUnitContext context)
        {
            Console.WriteLine("Global done");
            
            Console.WriteLine("Code after semantics and globals:");
            var a = new CompilationUnitCodeGenerator();
            var text = new AsmCodeWriter();
            text = a.GenerateCodeForContext(context, text);
            Console.WriteLine(text.AllCode);
        }
    }
}
