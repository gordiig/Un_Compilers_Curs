using Antlr4.Runtime;
using TestANTLR.Generators.Expressions.Logical;
using TestANTLR.Scopes;

namespace TestANTLR.Generators.Expressions
{
    public class TernaryExpressionGenerator: BaseCodeGenerator
    {
        private static int ternaryExprsCnt = 0;
        
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var ternaryExprCtx = context as MiniCParser.TernaryExpressionContext;
            var logicalOrExpression = ternaryExprCtx.logicalOrExpression();
            var ternaryExpressions = ternaryExprCtx.ternaryExpression();
            
            var logicalOrGen = new LogicalOrGenerator();
            // Logical or expr only
            if (ternaryExprCtx.Question() == null)
                currentCode = logicalOrGen.GenerateCodeForContext(logicalOrExpression, currentCode);
            // With parenthesis on right side
            else
            {
                currentCode.AddComment("Ternary predicate calculation");
                // Вычисление предиката
                currentCode = logicalOrGen.GenerateCodeForContext(logicalOrExpression, currentCode);
                var predicateResultRegister = currentCode.LastAssignedRegister;
                
                // Запись результата в предикатный регистр
                var type = SymbolType.GetType("int");    // TODO: TYPING
                var predicateRegister = currentCode.GetFreePredicateRegister();
                currentCode.AddCompareRegisterEqNumber(predicateRegister, predicateResultRegister, 
                    "0", type, true);
                currentCode.FreeRegister(predicateResultRegister);
                
                // Прыжок к нужной ветке тернарного выражения
                ternaryExprsCnt++;
                var labelTrue = $"ternary_{ternaryExprsCnt}_if";
                var labelFalse = $"ternary_{ternaryExprsCnt}_else";
                currentCode.AddConditionalJump(predicateRegister, labelTrue);
                currentCode.AddConditionalJump(predicateRegister, labelFalse, true);

                var ternaryExpressionGen = new TernaryExpressionGenerator();
                // True-ветка тернарного выражения
                currentCode.AddComment("Ternary true branch");
                currentCode.AddPlainCode($"{labelTrue}:");
                currentCode = ternaryExpressionGen.GenerateCodeForContext(ternaryExpressions[0], currentCode);
                var trueValueRegister = currentCode.LastAssignedRegister;
                
                // False-ветка тернарного выражения
                currentCode.AddComment("Ternary false branch");
                currentCode.AddPlainCode($"{labelFalse}:");
                currentCode = ternaryExpressionGen.GenerateCodeForContext(ternaryExpressions[1], currentCode);
                var falseValueRegister = currentCode.LastAssignedRegister;
                
                // Присваивание результата регистру
                currentCode.AddComment("Ternary result");
                var resultRegister = currentCode.GetFreeRegister();
                currentCode.AddConditionalRegisterToRegisterAssign(predicateRegister, resultRegister,
                    trueValueRegister, falseValueRegister);
                
                // Метка конца тернарного выражения (для простоты чтения асма)
                var labelEnd = $"ternary_{ternaryExprsCnt}_end";
                currentCode.AddPlainCode($"{labelEnd}:");
                
                // Чистка регистров
                currentCode.FreePredicateRegister(predicateRegister);
                currentCode.FreeRegister(trueValueRegister);
                currentCode.FreeRegister(falseValueRegister);
            }
            
            return currentCode;
        }
    }
}