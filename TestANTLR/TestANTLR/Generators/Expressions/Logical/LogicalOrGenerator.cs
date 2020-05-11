using Antlr4.Runtime;
using TestANTLR.Scopes;

namespace TestANTLR.Generators.Expressions.Logical
{
    public class LogicalOrGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var logicalOrExprCtx = context as MiniCParser.LogicalOrExpressionContext;
            var logicalAndExpression = logicalOrExprCtx.logicalAndExpression();
            var logicalOrExpression = logicalOrExprCtx.logicalOrExpression();
            
            var logicalAndGen = new LogicalAndGenerator();
            // With logical or expr
            if (logicalOrExpression != null)
            {
                currentCode.AddComment("Logical or operation");
                
                // Вычисление rvalue
                currentCode = logicalAndGen.GenerateCodeForContext(logicalAndExpression, currentCode);
                var rValueRegister = currentCode.LastAssignedRegister;
                
                // Привод типов если нужно
                var rValueTypeToConvert = currentCode.Conversions.Get(logicalAndExpression);
                if (rValueTypeToConvert != null)
                    currentCode.ConvertRegisterToType(rValueRegister, rValueRegister, 
                        rValueTypeToConvert);
                
                // Сравнение rvalue c 0
                var pRegister = currentCode.GetFreePredicateRegister();
                currentCode.AddCompareRegisterEqNumber(pRegister, rValueRegister, "0", true);
                var rValueCompareResultRegister = currentCode.GetFreeRegister();
                currentCode.AddRegisterToRegisterAssign(rValueCompareResultRegister, pRegister);

                // Вычисление lvalue
                var logicalOrGen = new LogicalOrGenerator();
                currentCode = logicalOrGen.GenerateCodeForContext(logicalOrExpression, currentCode);
                var lValueRegister = currentCode.LastAssignedRegister;
                
                // Привод типов если нужно
                var lValueTypeToConvert = currentCode.Conversions.Get(logicalOrExpression);
                if (lValueTypeToConvert != null)
                    currentCode.ConvertRegisterToType(lValueRegister, lValueRegister, 
                        lValueTypeToConvert);

                // Сравнение lvalue c 0
                currentCode.AddCompareRegisterEqNumber(pRegister, lValueRegister, "0", true);
                var lValueCompareResultRegister = currentCode.GetFreeRegister();
                currentCode.AddRegisterToRegisterAssign(lValueCompareResultRegister, pRegister);

                // Применение OR к результатам сравнения
                var resultRegister = currentCode.GetFreeRegister();
                currentCode.AddRegisterOrRegister(resultRegister, lValueRegister, rValueRegister);
                    
                // Чистка регистров
                currentCode.FreePredicateRegister(pRegister);
                currentCode.FreeRegister(lValueCompareResultRegister);
                currentCode.FreeRegister(lValueRegister);
                currentCode.FreeRegister(rValueCompareResultRegister);
                currentCode.FreeRegister(rValueRegister);
            }
            // Logical and expr only
            else
                currentCode = logicalAndGen.GenerateCodeForContext(logicalAndExpression, currentCode);

            return currentCode;
        }
    }
}