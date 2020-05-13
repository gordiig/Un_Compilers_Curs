using Antlr4.Runtime;
using MiniC.Generators.Expressions;

namespace MiniC.Generators.Statements
{
    public class JumpStatementGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var jumpStmCtx = context as MiniCParser.JumpStatementContext;
            var continue_ = jumpStmCtx.Continue();
            var break_ = jumpStmCtx.Break();
            var return_ = jumpStmCtx.Return();
            var ternaryExpression = jumpStmCtx.ternaryExpression();
            
            // Continue
            if (continue_ != null)
            {
                currentCode.AddComment("Continue operator");
                var currentLoopName = currentCode.LoopStack.Peek();
                currentCode.AddContinue(currentLoopName);
            }
            // Break
            else if (break_ != null)
            {
                currentCode.AddComment("Break operator");
                var currentLoopName = currentCode.LoopStack.Peek();
                currentCode.AddBreak(currentLoopName);
            }
            // Return
            else
            {
                // Если функция возвращает что-то
                if (ternaryExpression != null)
                {
                    // Получаем значение
                    currentCode.AddComment("Getting return value");
                    var ternaryExprGen = new TernaryExpressionGenerator();
                    currentCode = ternaryExprGen.GenerateCodeForContext(ternaryExpression, currentCode);
                    var lastAssignedRegister = getValueFromExpression(currentCode);
                    
                    // Привод типов если нужно
                    convertTypeIfNeeded(currentCode, lastAssignedRegister, ternaryExpression);

                    // Присваиваем r0 значение и return
                    currentCode.AddReturnValue(lastAssignedRegister);
                    currentCode.FreeRegister(lastAssignedRegister);
                }
                // Сам возврат
                currentCode.AddComment("Return operator");
                var currentFuncName = currentCode.GetCurrentFunc();
                currentCode.AddReturn(currentFuncName);
            }
            
            return currentCode;
        }
    }
}