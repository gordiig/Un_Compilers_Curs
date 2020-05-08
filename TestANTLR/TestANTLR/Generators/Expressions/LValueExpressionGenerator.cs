using Antlr4.Runtime;

namespace TestANTLR.Generators.Expressions
{
    public class LValueExpressionGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var lvalExprCtx = context as MiniCParser.LValueExpressionContext;
            var identifier = lvalExprCtx.Identifier();
            var ternaryExpr = lvalExprCtx.ternaryExpression();
            var lvalExprs = lvalExprCtx.lValueExpression();
            // Variable name (a ...)
            if (identifier != null)
            {
                currentCode.AddComment($"Getting variable \"{identifier.GetText()}\" for lvalue");
                var register = currentCode.GetFreeRegister();
                var type = "int";    // TODO: ADD GETTING TYPE FROM TABLE
                currentCode.AddVariableToRegisterReading(identifier.GetText(), type, register);
            }
            // Braces (a[] ...)
            else if (ternaryExpr != null)
            {
                currentCode.AddComment("Getting braces value");
                
                // Вычисления в скобках
                var terExprGen = new TernaryExpressionGenerator();
                currentCode = terExprGen.GenerateCodeForContext(ternaryExpr, currentCode);
                var inBracesValueRegister = currentCode.LastAssignedRegister;
                
                // Вычисление lValue
                var lvalExprGen = new LValueExpressionGenerator();
                currentCode = lvalExprGen.GenerateCodeForContext(lvalExprs[0], currentCode);
                var lValueRegister = currentCode.LastAssignedRegister;
                var arrayVarName = currentCode.LastReferencedVariable;
                
                // Вычисление оффсета для массива
                // TODO MULTIPLYING lvalExprGen * type.size
                currentCode.AddComment("Getting indexed value for lvalue");
                var offsetRegister = "TODO OFFSET REGISTER";
                
                // Само действие
                var lhsRegister = currentCode.GetFreeRegister();
                var type = "int";    // TODO: ADD GETTING TYPE FROM TABLE
                currentCode.AddArrayToRegisterReading(arrayVarName, type, lhsRegister,  offsetRegister);
                
                // Чистка регистров
                currentCode.FreeRegister(inBracesValueRegister);
                currentCode.FreeRegister(lValueRegister);
            }
            // Dot (a.x ...)
            else
            {
                // TODO STRUCTS
            }
            
            return currentCode;
        }
    }
}