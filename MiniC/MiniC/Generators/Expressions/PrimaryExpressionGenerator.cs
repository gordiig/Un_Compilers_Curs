using System;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using MiniC.Exceptions;
using MiniC.Scopes;

namespace MiniC.Generators.Expressions
{
    public class PrimaryExpressionGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            if (context is MiniCParser.PrimaryExpContext primaryExpContext)
                context = primaryExpContext.children[0] as ParserRuleContext;
            
            // Identifier
            if (context is MiniCParser.VarReadContext identifier)
            {
                currentCode.AddComment($"Getting variable {identifier.GetText()} address");
                
                // Достаем переменную из скоупа
                var currentScope = currentCode.GetCurrentScope();
                var symbol = currentScope.FindSymbol(identifier.GetText()) as VarSymbol;
                if (symbol == null)
                    throw new CodeGenerationException($"Unknown symbol {identifier.GetText()}");

                if (symbol.Type.IsStructType())
                    currentCode.LastReferencedStructType = symbol.Type;
                currentCode.LastReferencedSymbol = symbol;

                // Запись адреса в регистр
                var destRegister = currentCode.GetFreeRegister();
                currentCode.AddVariableAddressToRegisterReading(symbol, destRegister);
            }
            // Constant
            else if (context is MiniCParser.ConstReadContext constant)
            {
                currentCode.AddComment($"Getting constant {constant.GetText()}");
                
                // Выясняем тип литерала
                var constCtx = context.children[0] as MiniCParser.ConstantContext;
                SymbolType type = null;
                if (constCtx.FloatingConstant() != null)
                    type = SymbolType.GetType("float");
                else if (constCtx.IntegerConstant() != null)
                    type = SymbolType.GetType("int");
                else if (constCtx.CharacterConstant() != null)
                    type = SymbolType.GetType("char");
                else 
                    throw new CodeGenerationException($"Unknown literal found: {constant.GetText()}");
                
                // Запись в регистр
                var destRegister = currentCode.GetFreeRegister();
                currentCode.AddValueToRegisterAssign(destRegister, constant.GetText(), type);
            }
            // Expression
            else if (context is MiniCParser.ParensContext parensContext)
            {
                currentCode.AddComment("Getting parenthesis value");
                var ternaryExpression = parensContext.ternaryExpression();
                var expressionGen = new ExpressionGenerator();
                currentCode = expressionGen.GenerateCodeForContext(ternaryExpression, currentCode);
            }

            return currentCode;
        }
    }
}