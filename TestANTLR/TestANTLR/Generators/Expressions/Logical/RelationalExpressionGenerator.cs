using Antlr4.Runtime;
using TestANTLR.Generators.Expressions.BinaryOperators;

namespace TestANTLR.Generators.Expressions.Logical
{
    public class RelationalExpressionGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var relationalExprCtx = context as MiniCParser.RelationalExpressionContext;
            var shiftExpression = relationalExprCtx.shiftExpression();
            var relationalExpression = relationalExprCtx.relationalExpression();
            
            var shiftGenerator = new ShiftExpressionGenerator();
            // With relational expr
            if (relationalExpression != null)
            {
                // TODO: RELATIONAL OPERATORS
            }
            // Shift expr only
            else
            {
                currentCode = shiftGenerator.GenerateCodeForContext(shiftExpression, currentCode);
            }
            
            return currentCode;
        }
    }
}