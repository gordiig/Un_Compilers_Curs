using Antlr4.Runtime;
using MiniC.Exceptions;
using MiniC.Generators.Expressions.BinaryOperators;
using MiniC.Scopes;

namespace MiniC.Generators.Expressions.Logical
{
    public class RelationalExpressionGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var relationalExprCtx = context as MiniCParser.RelationalExpressionContext;
            var shiftExpression = relationalExprCtx.shiftExpression();
            var relationalExpression = relationalExprCtx.relationalExpression();
            
            var shiftGenerator = new ShiftExpressionGenerator();
            // With relational expr
            if (relationalExpression != null)
            {
                // Вычисляем rvalue
                currentCode = shiftGenerator.GenerateCodeForContext(shiftExpression, currentCode);
                var rValueRegister = getValueFromExpression(currentCode);
                
                // Привод типов если нужно
                convertTypeIfNeeded(currentCode, rValueRegister, shiftExpression);

                // Вычисляем lvalue
                var relationalGen = new RelationalExpressionGenerator();
                currentCode = relationalGen.GenerateCodeForContext(relationalExpression, currentCode);
                var lValueRegister = getValueFromExpression(currentCode);
                
                // Привод типов если нужно
                convertTypeIfNeeded(currentCode, lValueRegister, relationalExpression);

                // Применяем оператор
                var pRegister = currentCode.GetFreePredicateRegister();
                if (relationalExprCtx.Less() != null) 
                    currentCode.AddCompareRegisterLtRegister(pRegister, lValueRegister, rValueRegister);
                else if (relationalExprCtx.LessEqual() != null)
                    currentCode.AddCompareRegisterLeRegister(pRegister, lValueRegister, rValueRegister);
                else if (relationalExprCtx.Greater() != null)
                    currentCode.AddCompareRegisterGtRegister(pRegister, lValueRegister, rValueRegister);
                else if (relationalExprCtx.GreaterEqual() != null)
                    currentCode.AddCompareRegisterGeRegister(pRegister, lValueRegister, rValueRegister);
                else 
                    throw new CodeGenerationException("Unknown relational operator");
                
                // Переносим результат сравнения в регистр
                var resultRegister = currentCode.GetFreeRegister();
                currentCode.AddRegisterToRegisterAssign(resultRegister, pRegister);
                
                // Чистка регистров
                currentCode.FreePredicateRegister(pRegister);
                currentCode.FreeRegister(lValueRegister);
                currentCode.FreeRegister(rValueRegister);
            }
            // Shift expr only
            else
                currentCode = shiftGenerator.GenerateCodeForContext(shiftExpression, currentCode);

            return currentCode;
        }
    }
}