using Antlr4.Runtime;

namespace TestANTLR.Generators.Expressions
{
    public class ExpressionGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var expressionCtx = context as MiniCParser.ExpressionContext;
            var assignmentExpression = expressionCtx.assignmentExpression();
            
            var assignmentGen = new AssignmentExpressionGenerator();
            currentCode = assignmentGen.GenerateCodeForContext(assignmentExpression, currentCode);
            
            return currentCode;
        }
    }
}