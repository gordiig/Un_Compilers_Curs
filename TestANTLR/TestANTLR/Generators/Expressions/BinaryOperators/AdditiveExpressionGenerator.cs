using Antlr4.Runtime;

namespace TestANTLR.Generators.Expressions.BinaryOperators
{
    public class AdditiveExpressionGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var additiveExprCtx = context as MiniCParser.AdditiveExpressionContext;
            var additiveExpression = additiveExprCtx.additiveExpression();
            var multiplicativeExpression = additiveExprCtx.multiplicativeExpression();
            
            var multiplicativeGenerator = new MultiplicativeExpressionGenerator();
            // With additive expr
            if (additiveExpression != null)
            {
                // Вычисление rvalue
                currentCode = multiplicativeGenerator.GenerateCodeForContext(multiplicativeExpression, currentCode);
                var multiplicativeResultRegister = currentCode.LastAssignedRegister;
                
                // Вычисление lvalue
                var additiveGenerator = new AdditiveExpressionGenerator();
                currentCode = additiveGenerator.GenerateCodeForContext(additiveExpression, currentCode);
                var additiveResultRegister = currentCode.LastAssignedRegister;
                
                // Сама операция
                currentCode.AddComment("Doing additive operator");
                var destRegister = currentCode.GetFreeRegister();
                if (additiveExprCtx.Plus() != null)
                    currentCode.AddAddingRegisterToRegister(destRegister, additiveResultRegister, multiplicativeResultRegister);
                else
                    currentCode.AddSubRegisterFromRegister(destRegister, additiveResultRegister, multiplicativeResultRegister);
                
                // Чистка регистров
                currentCode.FreeRegister(multiplicativeResultRegister);
                currentCode.FreeRegister(additiveResultRegister);
            }
            // Multiplicative expr only
            else
                currentCode = multiplicativeGenerator.GenerateCodeForContext(multiplicativeExpression, currentCode);

            return currentCode;
        }
    }
}