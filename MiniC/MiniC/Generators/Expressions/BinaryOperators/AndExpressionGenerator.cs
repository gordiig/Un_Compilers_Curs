using Antlr4.Runtime;
using MiniC.Generators.Expressions.Logical;

namespace MiniC.Generators.Expressions.BinaryOperators
{
    public class AndExpressionGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var andExprCtx = context as MiniCParser.AndExpressionContext;
            var equalityExpression = andExprCtx.equalityExpression();
            var andExpression = andExprCtx.andExpression();
            
            var equalityGenerator = new EqualityExpressionGenerator();
            // With and expr
            if (andExpression != null)
            {
                // Вычисление rvalue
                currentCode = equalityGenerator.GenerateCodeForContext(equalityExpression, currentCode);
                var rValueRegister = getValueFromExpression(currentCode);
                
                // Привод типов если нужно
                convertTypeIfNeeded(currentCode, rValueRegister, equalityExpression);

                // Вычисление lvalue
                var andExpressionGen = new AndExpressionGenerator();
                currentCode = andExpressionGen.GenerateCodeForContext(andExpression, currentCode);
                var lValueRegister = getValueFromExpression(currentCode);
                
                // Привод типов если нужно
                convertTypeIfNeeded(currentCode, lValueRegister, andExpression);

                // Вычисление результата
                currentCode.AddComment("Doing & operator");
                var resultRegister = currentCode.GetFreeRegister();
                currentCode.AddRegisterAndRegister(resultRegister, lValueRegister, rValueRegister);
                
                // Чистка регистров
                currentCode.FreeRegister(rValueRegister);
                currentCode.FreeRegister(lValueRegister);
            }
            // Equality expr only
            else
            {
                currentCode = equalityGenerator.GenerateCodeForContext(equalityExpression, currentCode);
            }
            
            return currentCode;
        }
    }
}