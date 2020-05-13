using Antlr4.Runtime;

namespace MiniC.Generators.Expressions
{
    public class ExpressionGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var expressionCtx = context as MiniCParser.ExpressionContext;
            var assignmentExpression = expressionCtx.assignmentExpression();
            
            var assignmentGen = new AssignmentExpressionGenerator();
            currentCode = assignmentGen.GenerateCodeForContext(assignmentExpression, currentCode);
            
            if (currentCode.Conversions.Get(assignmentExpression) != null)
            {
                var typeToConvert = currentCode.Conversions.Get(assignmentExpression);
                var valueRegister = currentCode.LastAssignedRegister;
                currentCode.ConvertRegisterToType(valueRegister, valueRegister, typeToConvert);
            }
            
            return currentCode;
        }
    }
}