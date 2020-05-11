using Antlr4.Runtime;
using TestANTLR.Generators.Expressions;
using TestANTLR.Scopes;

namespace TestANTLR.Generators.Statements
{
    public class IfStatementGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var ifStmtCtx = context as MiniCParser.IfStatementContext;
            var ternaryExpression = ifStmtCtx.ternaryExpression();
            var statements = ifStmtCtx.statement();
            var hasElse = ifStmtCtx.Else() != null;

            // Начало ифа
            var ifName = "if_name_TODO";    // TODO: GETTING IF NAMES
            currentCode.AddIfStart(ifName);
            
            // Вычисление условия ифа
            var ternaryExpressionGenerator = new TernaryExpressionGenerator();
            currentCode = ternaryExpressionGenerator.GenerateCodeForContext(ternaryExpression, currentCode);
            var checkValueRegister = getValueFromExpression(currentCode);
            
            // Привод типов если нужен
            convertTypeIfNeeded(currentCode, checkValueRegister, ternaryExpression);
            
            // Получение предикатного регистра и его заполнение
            var type = SymbolType.GetType("int");     // TODO: TYPING
            var predicateRegister = currentCode.GetFreePredicateRegister();
            currentCode.AddCompareRegisterEqNumber(predicateRegister, checkValueRegister, "0", true);
            currentCode.FreeRegister(checkValueRegister);
            
            // Jump при нулевом предикатном регистре, и его чистка после джампа
            // Если нет блока else
            if (!hasElse) 
                currentCode.AddConditionalJumpToIfEnd(ifName, predicateRegister, true);
            // Если есть else
            else
                currentCode.AddConditionalJumpToElse(ifName, predicateRegister, true);
            currentCode.FreePredicateRegister(predicateRegister);
            
            // Основное тело ифа
            var statementGen = new StatementGenerator();
            currentCode = statementGen.GenerateCodeForContext(statements[0], currentCode);
            // TODO: ВОЗМОЖНО ТУТ СТОИТ ЧИСТИТЬ ПОСЛЕДНИЙ LHS РЕГИСТР
            
            // Добавляем else, если он есть
            if (hasElse)
            {
                currentCode.AddIfElse(ifName);
                currentCode = statementGen.GenerateCodeForContext(statements[1], currentCode);
                // TODO: ВОЗМОЖНО ТУТ СТОИТ ЧИСТИТЬ ПОСЛЕДНИЙ LHS РЕГИСТР
            }

            // Проставляем метку конца ифа
            currentCode.AddIfEnd(ifName);
            return currentCode;
        }
    }
}