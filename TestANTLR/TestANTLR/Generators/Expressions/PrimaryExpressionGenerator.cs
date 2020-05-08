using System;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace TestANTLR.Generators.Expressions
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
                currentCode.AddComment($"Getting variable \"{identifier.GetText()}\"");
                var destRegister = currentCode.GetFreeRegister();
                var type = "int";    // TODO: ADD GETTING TYPE
                currentCode.AddVariableToRegisterReading(identifier.GetText(), type, destRegister);
            }
            // Constant
            else if (context is MiniCParser.ConstReadContext constant)
            {
                currentCode.AddComment($"Getting constant {constant.GetText()}");
                var destRegister = currentCode.GetFreeRegister();
                currentCode.AddValueToRegisterAssign(destRegister, constant.GetText());
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