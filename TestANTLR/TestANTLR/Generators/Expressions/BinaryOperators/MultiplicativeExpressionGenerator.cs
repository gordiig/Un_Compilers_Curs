using System;
using Antlr4.Runtime;
using TestANTLR.Scopes;

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
                
                // Привод типа, если нужно
                var rValueTypeToConvert = currentCode.Conversions.Get(unaryExpression);
                if (rValueTypeToConvert != null)
                    currentCode.ConvertRegisterToType(rValueRegister, rValueRegister, 
                        rValueTypeToConvert);
                
                // Вычисление lvalue
                var multiplicativeExpressionGen = new MultiplicativeExpressionGenerator();
                currentCode = multiplicativeExpressionGen.GenerateCodeForContext(multiplicativeExpression, currentCode);
                var lValueRegister = currentCode.LastAssignedRegister;
                
                // Привод типа, если нужно
                var lValueTypeToConvert = currentCode.Conversions.Get(multiplicativeExpression);
                if (lValueTypeToConvert != null)
                    currentCode.ConvertRegisterToType(lValueRegister, lValueRegister, 
                        lValueTypeToConvert);
                
                // Вычисление
                currentCode.AddComment("Doing some multiplicative operator");
                var resultRegister = currentCode.GetFreeRegister();
                if (multiplicativeExprCtx.Star() != null) 
                    currentCode.AddRegisterMpyRegister(resultRegister, lValueRegister, rValueRegister);
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