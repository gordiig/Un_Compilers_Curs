using Antlr4.Runtime;
using MiniC.Scopes;

namespace MiniC.Generators.Expressions
{
    public class UnaryExpressionGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var unaryExprCtx = context as MiniCParser.UnaryExpressionContext;
            var postfixExpression = unaryExprCtx.postfixExpression();
            var unaryOperator = unaryExprCtx.unaryOperator();
            var unaryExpression = unaryExprCtx.unaryExpression();
            
            // Postfix expr only
            if (postfixExpression != null)
            {
                // Полчучаем адрес переменной справа, либо значение константы
                var postfixExpressionGenerator = new PostfixExpressionGenerator();
                currentCode = postfixExpressionGenerator.GenerateCodeForContext(postfixExpression, currentCode);
            }
            // With unary operator
            else
            {
                // Вычисление значения для проведения унарной операции
                currentCode.AddComment("Value for unary operation:");
                var unaryExpressionGenerator = new UnaryExpressionGenerator();
                currentCode = unaryExpressionGenerator.GenerateCodeForContext(unaryExpression, currentCode);
                var valueForOperationRegister = getValueFromExpression(currentCode);
                
                // Привод типов, если нужен
                convertTypeIfNeeded(currentCode, valueForOperationRegister, unaryExpression);

                // Применение операции
                var resultRegister = currentCode.GetFreeRegister();
                if (unaryOperator.Plus() != null)
                {
                    currentCode.AddComment("Unary +");
                    currentCode.AddRegisterToRegisterAssign(resultRegister, valueForOperationRegister);
                }
                else if (unaryOperator.Minus() != null)
                {
                    currentCode.AddComment("Unary -");
                    currentCode.AddNegateRegister(resultRegister, valueForOperationRegister);
                }
                else if (unaryOperator.Tilde() != null)
                {
                    currentCode.AddComment("Unary ~");
                    currentCode.AddNotRegister(resultRegister, valueForOperationRegister);
                }
                else
                {
                    currentCode.AddComment("Unary !");
                    // Проверка на ноль
                    var type = SymbolType.GetType("int");    // TODO: TYPING
                    var predicateRegister = currentCode.GetFreePredicateRegister();
                    currentCode.AddCompareRegisterEqNumber(predicateRegister, valueForOperationRegister, "0");
                    
                    // Создаем "нулевой" регистр
                    var zeroRegister = currentCode.GetFreeRegister();
                    currentCode.AddValueToRegisterAssign(zeroRegister, "0", type);    // TODO: TYPING
                    
                    // Создаем "единичный регистр"
                    var oneRegister = currentCode.GetFreeRegister();
                    currentCode.AddValueToRegisterAssign(oneRegister, "1", type);    // TODO: TYPING
                    
                    // Если проверка на 0 успешна, то ноль, иначе 1
                    currentCode.AddConditionalRegisterToRegisterAssign(predicateRegister,
                        resultRegister, zeroRegister, oneRegister);
                    
                    // Чистка регистров
                    currentCode.FreePredicateRegister(predicateRegister);
                    currentCode.FreeRegister(zeroRegister);
                    currentCode.FreeRegister(oneRegister);
                }

                // Чистка регистров
                currentCode.FreeRegister(valueForOperationRegister);
            }

            return currentCode;
        }
    }
}