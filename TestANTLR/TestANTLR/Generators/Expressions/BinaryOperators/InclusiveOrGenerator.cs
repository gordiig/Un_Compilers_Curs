using Antlr4.Runtime;

namespace TestANTLR.Generators.Expressions.BinaryOperators
{
    public class InclusiveOrGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var inclusiveOrExprCtx = context as MiniCParser.InclusiveOrExpressionContext;
            var exclisiveOrExpression = inclusiveOrExprCtx.exclusiveOrExpression();
            var inclusiveOrExpression = inclusiveOrExprCtx.inclusiveOrExpression();
            
            var exclusiveOrGenerator = new ExclusiveOrGenerator();
            // With inclusive or expr 
            if (inclusiveOrExpression != null)
            {
                // Вычисление rvalue
                currentCode = exclusiveOrGenerator.GenerateCodeForContext(exclisiveOrExpression, currentCode);
                var rValueRegister = currentCode.LastAssignedRegister;
                
                // Привод типов если нужно
                var rValueTypeToConvert = currentCode.Conversions.Get(exclisiveOrExpression);
                if (rValueTypeToConvert != null)
                    currentCode.ConvertRegisterToType(rValueRegister, rValueRegister, 
                        rValueTypeToConvert);
                
                // Вычисление lvalue
                var inclusiveOrGen = new InclusiveOrGenerator();
                currentCode = inclusiveOrGen.GenerateCodeForContext(inclusiveOrExpression, currentCode);
                var lValueRegister = currentCode.LastAssignedRegister;
                
                // Привод типов если нужно
                var lValueTypeToConvert = currentCode.Conversions.Get(inclusiveOrExpression);
                if (lValueTypeToConvert != null)
                    currentCode.ConvertRegisterToType(lValueRegister, lValueRegister, 
                        lValueTypeToConvert);
                
                // Вычисление результата
                currentCode.AddComment("Doing | operator");
                var resultRegister = currentCode.GetFreeRegister();
                currentCode.AddRegisterOrRegister(resultRegister, lValueRegister, rValueRegister);

                // Чистка регистров
                currentCode.FreeRegister(rValueRegister);
                currentCode.FreeRegister(lValueRegister);
            }
            // Exclusive or only
            else
                currentCode = exclusiveOrGenerator.GenerateCodeForContext(exclisiveOrExpression, currentCode);

            return currentCode;
        }
    }
}