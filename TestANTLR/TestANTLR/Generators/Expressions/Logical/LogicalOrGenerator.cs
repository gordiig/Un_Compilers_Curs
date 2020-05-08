using Antlr4.Runtime;

namespace TestANTLR.Generators.Expressions.Logical
{
    public class LogicalOrGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var logicalOrExprCtx = context as MiniCParser.LogicalOrExpressionContext;
            var logicalAndExpression = logicalOrExprCtx.logicalAndExpression();
            var logicalOrExpression = logicalOrExprCtx.logicalOrExpression();
            
            var logicalAndGen = new LogicalAndGenerator();
            // With logical or expr
            if (logicalOrExpression != null)
            {
                // TODO: ADD || OPERATOR
            }
            // Logical and expr only
            else
            {
                currentCode = logicalAndGen.GenerateCodeForContext(logicalAndExpression, currentCode);
            }

            return currentCode;
        }
    }
}