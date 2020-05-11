using Antlr4.Runtime;

namespace TestANTLR.Generators.Expressions.BinaryOperators
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
                var rValueRegister = currentCode.LastAssignedRegister;
                
                // Привод типов если нужно
                var rValueTypeToConvert = currentCode.Conversions.Get(andExpression);
                if (rValueTypeToConvert != null)
                    currentCode.ConvertRegisterToType(rValueRegister, rValueRegister, 
                        rValueTypeToConvert);
                
                // Вычисление lvalue
                var exclusiveOrGen = new ExclusiveOrGenerator();
                currentCode = exclusiveOrGen.GenerateCodeForContext(exclusiveOrExpression, currentCode);
                var lValueRegister = currentCode.LastAssignedRegister;

                // Привод типов если нужно
                var lValueTypeToConvert = currentCode.Conversions.Get(exclusiveOrExpression);
                if (lValueTypeToConvert != null)
                    currentCode.ConvertRegisterToType(lValueRegister, lValueRegister, 
                        lValueTypeToConvert);
                
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