using Antlr4.Runtime;
using TestANTLR.Generators.Expressions;
using TestANTLR.Scopes;

namespace TestANTLR.Generators.Statements
{
    public class IterationStatementGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var iterationStmtCtx = context as MiniCParser.IterationStatementContext;
            var isDoWhile = iterationStmtCtx.Do() != null;
            var isFor = iterationStmtCtx.For() != null;
            
            // Если цикл for, то инициалиация должна быть вне тела цикла
            if (isFor && iterationStmtCtx.expression()[0] != null)
            {
                var expressionGen = new ExpressionGenerator();
                currentCode.AddComment("For loop init statement:");
                currentCode = expressionGen.GenerateCodeForContext(iterationStmtCtx.expression()[0], currentCode);
                // Чистим сразу, поскольку это значение нам не нужно
                currentCode.FreeRegister(currentCode.LastAssignedRegister);
            }
            
            // Начало цикла
            var loopName = "loop_name_TODO";
            currentCode.AddLoopStart(loopName);

            // Выбор алгоритма генерации в зависимости от типа цикла
            if (isDoWhile)
                currentCode = generateDoWileLoop(iterationStmtCtx, currentCode, loopName);
            else if (isFor)
                currentCode = generateForLoop(iterationStmtCtx, currentCode, loopName);
            else
                currentCode = generateWileLoop(iterationStmtCtx, currentCode, loopName);
            
            // Конец цикла
            currentCode.AddLoopEnd(loopName);
            return currentCode;
        }

        private AsmCodeWriter generateDoWileLoop(MiniCParser.IterationStatementContext context,
            AsmCodeWriter currentCode, string loopName)
        {
            // Тело цикла
            currentCode.AddComment("Do-while loop body:");
            var statementGen = new StatementGenerator();
            currentCode = statementGen.GenerateCodeForContext(context.statement(), currentCode);
            // TODO: ВОЗМОЖНО ТУТ СТОИТ ЧИСТИТЬ ПОСЛЕДНИЙ LHS РЕГИСТР

            // Проверка на возврат в начало цикла
            currentCode.AddComment("Do-while loop check for exit:");
            var ternaryExpressionGen = new TernaryExpressionGenerator();
            currentCode = ternaryExpressionGen.GenerateCodeForContext(context.ternaryExpression(), currentCode);
            var checkValueRegister = currentCode.LastAssignedRegister;
            
            // Перенос значения в предикатный регистр
            var type = SymbolType.GetType("int");    // TODO: TYPING
            var predicateRegister = currentCode.GetFreePredicateRegister();
            currentCode.AddCompareRegisterEqNumber(predicateRegister, checkValueRegister, "0", 
                type, true);
            currentCode.FreeRegister(checkValueRegister);
            
            // Джамп в начало цикла если нужно
            currentCode.AddConditionalContinue(loopName, predicateRegister, false);
            currentCode.FreePredicateRegister(predicateRegister);
            
            return currentCode;
        }
        
        private AsmCodeWriter generateWileLoop(MiniCParser.IterationStatementContext context,
            AsmCodeWriter currentCode, string loopName)
        {
            // Проверка на прыжок в конец цикла
            currentCode.AddComment("While loop check for exit:");
            var ternaryExpressionGen = new TernaryExpressionGenerator();
            currentCode = ternaryExpressionGen.GenerateCodeForContext(context.ternaryExpression(), currentCode);
            var checkValueRegister = currentCode.LastAssignedRegister;
            
            // Перенос значения в предикатный регистр
            var type = SymbolType.GetType("int");    // TODO: TYPING
            var predicateRegister = currentCode.GetFreePredicateRegister();
            currentCode.AddCompareRegisterEqNumber(predicateRegister, checkValueRegister, "0", 
                type, true);
            currentCode.FreeRegister(checkValueRegister);

            // Джамп в конец цикла если нужно
            currentCode.AddConditionalBreak(loopName, predicateRegister, true);
            currentCode.FreePredicateRegister(predicateRegister);
            
            // Основное тело цикла
            currentCode.AddComment("While loop body:");
            var statementGen = new StatementGenerator();
            currentCode = statementGen.GenerateCodeForContext(context.statement(), currentCode);
            // TODO: ВОЗМОЖНО ТУТ СТОИТ ЧИСТИТЬ ПОСЛЕДНИЙ LHS РЕГИСТР
            
            // Джамп в начало цикла
            currentCode.AddContinue(loopName);
            
            return currentCode;
        }
        
        private AsmCodeWriter generateForLoop(MiniCParser.IterationStatementContext context,
            AsmCodeWriter currentCode, string loopName)
        {
            // Условие выхода из цикла
            if (context.expression()[0] != null)
            {
                // Проверка на прыжок в конец цикла
                currentCode.AddComment("For loop check for exit");
                var ternaryExpressionGen = new TernaryExpressionGenerator();
                currentCode = ternaryExpressionGen.GenerateCodeForContext(context.ternaryExpression(), currentCode);
                var checkValueRegister = currentCode.LastAssignedRegister;
                
                // Перенос значения в предикатный регистр
                var type = SymbolType.GetType("int");    // TODO: TYPING
                var predicateRegister = currentCode.GetFreePredicateRegister();
                currentCode.AddCompareRegisterEqNumber(predicateRegister, checkValueRegister, "0", 
                    type, true);
                currentCode.FreeRegister(checkValueRegister);
                
                // Джамп в конец цикла если нужно
                currentCode.AddConditionalBreak(loopName, predicateRegister, true);
                currentCode.FreePredicateRegister(predicateRegister);
            }
            
            // Основное тело цикла
            currentCode.AddComment("For loop body:");
            var statementGen = new StatementGenerator();
            currentCode = statementGen.GenerateCodeForContext(context.statement(), currentCode);
            // TODO: ВОЗМОЖНО ТУТ СТОИТ ЧИСТИТЬ ПОСЛЕДНИЙ LHS РЕГИСТР
            
            // Инкремент счетчика
            if (context.expression()[1] != null)
            {
                currentCode.AddComment("For loop increments:");
                var expressionGen = new ExpressionGenerator();
                currentCode = expressionGen.GenerateCodeForContext(context.expression()[1], currentCode);
                // Чистим сразу -- нам эти значения не нужны
                currentCode.FreeRegister(currentCode.LastAssignedRegister);
            }
            
            // Джамп в начало цикла
            currentCode.AddContinue(loopName);

            return currentCode;
        }
    }
}