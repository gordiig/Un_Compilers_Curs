using Antlr4.Runtime;
using TestANTLR.Generators.Expressions.BinaryOperators;
using TestANTLR.Scopes;

namespace TestANTLR.Generators.Expressions.Logical
{
    public class LogicalAndGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var logicalAndExprCtx = context as MiniCParser.LogicalAndExpressionContext;
            var inclusiveOrExpression = logicalAndExprCtx.inclusiveOrExpression();
            var logicalAndExpression = logicalAndExprCtx.logicalAndExpression();
            
            var inclusiveOrGen = new InclusiveOrGenerator();
            // With logical and expr
            if (logicalAndExpression != null)
            {
                currentCode.AddComment("Logical and operation");
                
                // Вычисление rvalue
                currentCode = inclusiveOrGen.GenerateCodeForContext(inclusiveOrExpression, currentCode);
                var rValueRegister = currentCode.LastAssignedRegister;
                var rValueType = SymbolType.GetType("int");    // TODO: TYPING
                
                // Сравнение rvalue c 0
                var pRegister = currentCode.GetFreePredicateRegister();
                currentCode.AddCompareRegisterEqNumber(pRegister, rValueRegister, "0", rValueType, true);
                var rValueCompareResultRegister = currentCode.GetFreeRegister();
                currentCode.AddRegisterToRegisterAssign(rValueCompareResultRegister, pRegister);

                // Вычисление lvalue
                var logicalOrGen = new LogicalAndGenerator();
                currentCode = logicalOrGen.GenerateCodeForContext(logicalAndExpression, currentCode);
                var lValueRegister = currentCode.LastAssignedRegister;
                var lValueType = SymbolType.GetType("int");    // TODO: TYPING

                // Сравнение lvalue c 0
                currentCode.AddCompareRegisterEqNumber(pRegister, lValueRegister, "0", lValueType, true);
                var lValueCompareResultRegister = currentCode.GetFreeRegister();
                currentCode.AddRegisterToRegisterAssign(lValueCompareResultRegister, pRegister);

                // Применение AND к результатам сравнения
                var resultRegister = currentCode.GetFreeRegister();
                currentCode.AddRegisterAndRegister(resultRegister, lValueRegister, rValueRegister);
                    
                // Чистка регистров
                currentCode.FreePredicateRegister(pRegister);
                currentCode.FreeRegister(lValueCompareResultRegister);
                currentCode.FreeRegister(lValueRegister);
                currentCode.FreeRegister(rValueCompareResultRegister);
                currentCode.FreeRegister(rValueRegister);
            }
            // InclusiveOrOnly
            else
                currentCode = inclusiveOrGen.GenerateCodeForContext(inclusiveOrExpression, currentCode);
            
            return currentCode;
        }
    }
}