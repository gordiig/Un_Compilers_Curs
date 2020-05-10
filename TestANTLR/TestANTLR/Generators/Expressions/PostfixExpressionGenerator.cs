using Antlr4.Runtime;
using TestANTLR.Scopes;

namespace TestANTLR.Generators.Expressions
{
    public class PostfixExpressionGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            // Primary expr only
            if (context is MiniCParser.PrimaryExpContext primaryExpression)
            {
                var primaryExprGen = new PrimaryExpressionGenerator();
                currentCode = primaryExprGen.GenerateCodeForContext(primaryExpression, currentCode);
            }
            // Indexing (a[i])
            else if (context is MiniCParser.ArrayReadContext arrayReadContext)
            {
                currentCode.AddComment("Getting braces value");
                
                // Вычисление значения в скобках
                var ternaryExpression = arrayReadContext.ternaryExpression();
                var ternaryExpressionGen = new TernaryExpressionGenerator();
                currentCode = ternaryExpressionGen.GenerateCodeForContext(ternaryExpression, currentCode);
                var inBracesValueRegister = currentCode.LastAssignedRegister;
                
                // Получение переменной
                var postfixExpression = arrayReadContext.postfixExpression();
                var postfixExpressionGen = new PostfixExpressionGenerator();
                currentCode = postfixExpressionGen.GenerateCodeForContext(postfixExpression, currentCode);
                var variableValueRegister = currentCode.LastAssignedRegister;
                var variableName = currentCode.LastReferencedVariable;
                
                // Вычисление смещения
                // TODO: GETTING TYPES AND MULTIPLICATION
                currentCode.AddComment("Getting indexed value");
                var offsetRegister = "TODO OFFSET REGISTER";
                
                // Получение значения по индексу
                var lhsRegister = currentCode.GetFreeRegister();
                var type = SymbolType.GetType("int"); // TODO: TYPING
                currentCode.AddArrayToRegisterReading(variableName, type, lhsRegister, offsetRegister);
                
                // Освобождение регистров
                currentCode.FreeRegister(variableValueRegister);
                currentCode.FreeRegister(inBracesValueRegister);
            }
            // Function call (f(*))
            else if (context is MiniCParser.FunctionCallContext functionCallContext)
            {
                // TODO TERNARY EXPR
            }
            // Dotting (a.b)
            else
            {
                // TODO STRUCTS
            }
            
            return currentCode;
        }
    }
}