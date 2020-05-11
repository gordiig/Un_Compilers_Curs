using Antlr4.Runtime;
using TestANTLR.Scopes;

namespace TestANTLR.Generators.Expressions.Logical
{
    public class EqualityExpressionGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var equalityExprCtx = context as MiniCParser.EqualityExpressionContext;
            var relationalExpression = equalityExprCtx.relationalExpression();
            var equalityExpression = equalityExprCtx.equalityExpression();
            
            var relationalGenerator = new RelationalExpressionGenerator();
            // With equality expr
            if (equalityExpression != null)
            {
                // Вычисление значения справа
                currentCode = relationalGenerator.GenerateCodeForContext(relationalExpression, currentCode);
                var rValueRegister = currentCode.LastAssignedRegister;
                
                // Привод типов если нужно
                var rValueTypeToConvert = currentCode.Conversions.Get(relationalExpression);
                if (rValueTypeToConvert != null)
                    currentCode.ConvertRegisterToType(rValueRegister, rValueRegister, 
                        rValueTypeToConvert);
                
                // Вычисление значения слева
                var equalityGen = new EqualityExpressionGenerator();
                currentCode = equalityGen.GenerateCodeForContext(equalityExpression, currentCode);
                var lValueRegister = currentCode.LastAssignedRegister;
                
                // Привод типов если нужно
                var lValueTypeToConvert = currentCode.Conversions.Get(equalityExpression);
                if (lValueTypeToConvert != null)
                    currentCode.ConvertRegisterToType(lValueRegister, lValueRegister, 
                        lValueTypeToConvert);
                
                // Сравнение
                currentCode.AddComment("Equality comparing");
                var negate = equalityExprCtx.Equal() == null;
                var pRegister = currentCode.GetFreePredicateRegister();
                currentCode.AddCompareRegisterEqRegister(pRegister, lValueRegister, rValueRegister, negate);
                
                // Чистка регистров
                currentCode.FreeRegister(rValueRegister);
                currentCode.FreeRegister(lValueRegister);
                
                // Перенос результата в регистр
                var resultRegister = currentCode.GetFreeRegister();
                currentCode.AddRegisterToRegisterAssign(resultRegister, pRegister);
                currentCode.FreePredicateRegister(pRegister);
            }
            // Relational expr only
            else
                currentCode = relationalGenerator.GenerateCodeForContext(relationalExpression, currentCode);

            return currentCode;
        }
    }
}