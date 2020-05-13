using Antlr4.Runtime;
using MiniC.Generators.Expressions;

namespace MiniC.Generators.Statements
{
    public class ExpressionStatementGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var exprStmtCtx = context as MiniCParser.ExpressionStatementContext;
            
            // Expression
            if (exprStmtCtx.expression() != null)
            {
                var expressionGenerator = new ExpressionGenerator();
                currentCode = expressionGenerator.GenerateCodeForContext(exprStmtCtx.expression(), currentCode);
            }
            
            return currentCode;
        }
    }
}