using Antlr4.Runtime;
using TestANTLR.Generators.Expressions.Logical;

namespace TestANTLR.Generators.Expressions.BinaryOperators
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
                var rValueRegister = currentCode.LastAssignedRegister;
                
                // Привод типов если нужно
                var rValueTypeToConvert = currentCode.Conversions.Get(equalityExpression);
                if (rValueTypeToConvert != null)
                    currentCode.ConvertRegisterToType(rValueRegister, rValueRegister, 
                        rValueTypeToConvert);
                
                // Вычисление lvalue
                var andExpressionGen = new AndExpressionGenerator();
                currentCode = andExpressionGen.GenerateCodeForContext(andExpression, currentCode);
                var lValueRegister = currentCode.LastAssignedRegister;
                
                // Привод типов если нужно
                var lValueTypeToConvert = currentCode.Conversions.Get(andExpression);
                if (lValueTypeToConvert != null)
                    currentCode.ConvertRegisterToType(lValueRegister, lValueRegister, 
                        lValueTypeToConvert);
                
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