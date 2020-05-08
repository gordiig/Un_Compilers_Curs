using System;
using Antlr4.Runtime;

namespace TestANTLR.Generators.Expressions.BinaryOperators
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
                var rValueRegister = currentCode.LastAssignedRegister;
                
                // Вычисление lvalue
                var multiplicativeExpressionGen = new MultiplicativeExpressionGenerator();
                currentCode = multiplicativeExpressionGen.GenerateCodeForContext(multiplicativeExpression, currentCode);
                var lValueRegister = currentCode.LastAssignedRegister;
                
                // Вычисление
                var type = "int";    // TODO: TYPING
                currentCode.AddComment("Doing some multiplicative operator");
                var resultRegister = currentCode.GetFreeRegister();
                if (multiplicativeExprCtx.Star() != null) 
                    currentCode.AddRegisterMpyRegister(resultRegister, lValueRegister, rValueRegister, type);
                else if (multiplicativeExprCtx.Div() != null)
                    // TODO: DIV
                    throw new NotImplementedException("Div");
                else if (multiplicativeExprCtx.Mod() != null) 
                    // TODO: MOD
                    throw new NotImplementedException("Mod");
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