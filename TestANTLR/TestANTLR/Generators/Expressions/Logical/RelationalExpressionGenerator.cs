using Antlr4.Runtime;
using TestANTLR.Exceptions;
using TestANTLR.Generators.Expressions.BinaryOperators;
using TestANTLR.Scopes;

namespace TestANTLR.Generators.Expressions.Logical
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
                var rValueRegister = currentCode.LastAssignedRegister;
                
                // Вычисляем lvalue
                var relationalGen = new RelationalExpressionGenerator();
                currentCode = relationalGen.GenerateCodeForContext(relationalExpression, currentCode);
                var lValueRegister = currentCode.LastAssignedRegister;
                
                // Применяем оператор
                var pRegister = currentCode.GetFreePredicateRegister();
                var type = SymbolType.GetType("int");    // TODO: TYPING
                if (relationalExprCtx.Less() != null) 
                    currentCode.AddCompareRegisterLtRegister(pRegister, lValueRegister, rValueRegister, type);
                else if (relationalExprCtx.LessEqual() != null)
                    currentCode.AddCompareRegisterLeRegister(pRegister, lValueRegister, rValueRegister, type);
                else if (relationalExprCtx.Greater() != null)
                    currentCode.AddCompareRegisterGtRegister(pRegister, lValueRegister, rValueRegister, type);
                else if (relationalExprCtx.GreaterEqual() != null)
                    currentCode.AddCompareRegisterGeRegister(pRegister, lValueRegister, rValueRegister, type);
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