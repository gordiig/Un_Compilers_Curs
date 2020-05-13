using System;
using Antlr4.Runtime;
using MiniC.Scopes;

namespace MiniC.Generators.Expressions.BinaryOperators
{
    public class MultiplicativeExpressionGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var multiplicativeExprCtx = context as MiniCParser.MultiplicativeExpressionContext;
            var unaryExpression = multiplicativeExprCtx.unaryExpression();
            var multiplicativeExpression = multiplicativeExprCtx.multiplicativeExpression();
            
            var unaryExpressionGenerator = new UnaryExpressionGenerator();
            // With multiplicative expr
            if (multiplicativeExpression != null)
            {
                // Вычисление rvalue
                currentCode = unaryExpressionGenerator.GenerateCodeForContext(unaryExpression, currentCode);
                var rValueRegister = getValueFromExpression(currentCode);
                
                // Привод типа, если нужно
                convertTypeIfNeeded(currentCode, rValueRegister, unaryExpression);

                // Вычисление lvalue
                var multiplicativeExpressionGen = new MultiplicativeExpressionGenerator();
                currentCode = multiplicativeExpressionGen.GenerateCodeForContext(multiplicativeExpression, currentCode);
                var lValueRegister = getValueFromExpression(currentCode);
                
                // Привод типа, если нужно
                convertTypeIfNeeded(currentCode, lValueRegister, multiplicativeExpression);

                // Вычисление
                currentCode.AddComment("Doing some multiplicative operator");
                var resultRegister = currentCode.GetFreeRegister();
                if (multiplicativeExprCtx.Star() != null) 
                    currentCode.AddRegisterMpyRegister(resultRegister, lValueRegister, rValueRegister);
                else if (multiplicativeExprCtx.Div() != null)
                    currentCode.AddRegisterDivRegister(resultRegister, lValueRegister, rValueRegister);
                else if (multiplicativeExprCtx.Mod() != null) 
                    currentCode.AddRegisterModRegister(resultRegister, lValueRegister, rValueRegister);
                else 
                    throw new ApplicationException("Can't be here");
                
                // Чистка регистров
                currentCode.FreeRegister(rValueRegister);
                currentCode.FreeRegister(lValueRegister);
            }
            // Unary expr only
            else
                currentCode = unaryExpressionGenerator.GenerateCodeForContext(unaryExpression, currentCode);

            return currentCode;
        }
    }
}