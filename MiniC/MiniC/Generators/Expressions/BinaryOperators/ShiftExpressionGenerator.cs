using Antlr4.Runtime;

namespace MiniC.Generators.Expressions.BinaryOperators
{
    public class ShiftExpressionGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var shiftExprCtx = context as MiniCParser.ShiftExpressionContext;
            var additiveExpression = shiftExprCtx.additiveExpression();
            var shiftExpression = shiftExprCtx.shiftExpression();

            var additiveGenerator = new AdditiveExpressionGenerator();
            // With shift expr
            if (shiftExpression != null)
            {
                // Вычисление rvalue
                currentCode = additiveGenerator.GenerateCodeForContext(additiveExpression, currentCode);
                var rValueRegister = getValueFromExpression(currentCode);
                
                // Привод типов если нужно
                convertTypeIfNeeded(currentCode, rValueRegister, additiveExpression);

                // Вычисление lvalue
                var shiftExpressionGen = new ShiftExpressionGenerator();
                currentCode = shiftExpressionGen.GenerateCodeForContext(shiftExpression, currentCode);
                var lValueRegister = getValueFromExpression(currentCode);
                
                // Привод типов если нужно
                convertTypeIfNeeded(currentCode, lValueRegister, shiftExpression);

                // Вычисление результата
                currentCode.AddComment("Shifting");
                var resultRegister = currentCode.GetFreeRegister();
                if (shiftExprCtx.LeftShift() != null)
                    currentCode.AddRegisterLefShiftRegister(resultRegister, lValueRegister, rValueRegister);
                else 
                    currentCode.AddRegisterRightShiftRegister(resultRegister, lValueRegister, rValueRegister);
                
                // Чистка регистров
                currentCode.FreeRegister(rValueRegister);
                currentCode.FreeRegister(lValueRegister);
            }
            // Additive expr only
            else
                currentCode = additiveGenerator.GenerateCodeForContext(additiveExpression, currentCode);

            return currentCode;
        }
    }
}