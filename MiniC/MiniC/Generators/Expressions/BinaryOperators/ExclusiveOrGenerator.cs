using Antlr4.Runtime;

namespace MiniC.Generators.Expressions.BinaryOperators
{
    public class ExclusiveOrGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var exclusiveOrExprCtx = context as MiniCParser.ExclusiveOrExpressionContext;
            var exclusiveOrExpression = exclusiveOrExprCtx.exclusiveOrExpression();
            var andExpression = exclusiveOrExprCtx.andExpression();
            
            var andGenerator = new AndExpressionGenerator();
            // With exclusive or expr
            if (exclusiveOrExpression != null)
            {
                // Вычисление rvalue
                currentCode = andGenerator.GenerateCodeForContext(andExpression, currentCode);
                var rValueRegister = getValueFromExpression(currentCode);
                
                // Привод типов если нужно
                convertTypeIfNeeded(currentCode, rValueRegister, andExpression);

                // Вычисление lvalue
                var exclusiveOrGen = new ExclusiveOrGenerator();
                currentCode = exclusiveOrGen.GenerateCodeForContext(exclusiveOrExpression, currentCode);
                var lValueRegister = getValueFromExpression(currentCode);

                // Привод типов если нужно
                convertTypeIfNeeded(currentCode, lValueRegister, exclusiveOrExpression);

                // Вычисление результата
                currentCode.AddComment("Doing ^ operator");
                var resultRegister = currentCode.GetFreeRegister();
                currentCode.AddRegisterXorRegister(resultRegister, lValueRegister, rValueRegister);

                // Чистка регистров
                currentCode.FreeRegister(rValueRegister);
                currentCode.FreeRegister(lValueRegister);
            }
            // And expr only
            else
                currentCode = andGenerator.GenerateCodeForContext(andExpression, currentCode);

            return currentCode;
        }
    }
}