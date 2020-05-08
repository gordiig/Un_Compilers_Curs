using Antlr4.Runtime;
using TestANTLR.Generators.Expressions.BinaryOperators;

namespace TestANTLR.Generators.Expressions.Logical
{
    public class LogicalAndGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var logicalAndExprCtx = context as MiniCParser.LogicalAndExpressionContext;
            var inclusiveOrExpression = logicalAndExprCtx.inclusiveOrExpression();
            var logicalAndExpression = logicalAndExprCtx.logicalAndExpression();
            
            var inclusiveOrGen = new InclusiveOrGenerator();
            // With logical and expr
            if (logicalAndExpression != null)
            {
                // TODO: ADD && OPERATOR
            }
            // InclusiveOrOnly
            else
            {
                currentCode = inclusiveOrGen.GenerateCodeForContext(inclusiveOrExpression, currentCode);
            }
            
            return currentCode;
        }
    }
}