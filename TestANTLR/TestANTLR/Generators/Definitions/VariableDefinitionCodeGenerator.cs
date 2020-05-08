using Antlr4.Runtime;
using TestANTLR.Generators.Expressions;

namespace TestANTLR.Generators
{
    public class VariableDefinitionCodeGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var varDefCtx = context as MiniCParser.VarDefinitionContext;
            
            // Получаем инфу о переменной
            var header = varDefCtx.varHeader();
            var type = header.typeSpecifier().GetText();
            var identifier = header.Identifier().GetText();
            
            // Деларируем переменную со значением 0
            currentCode.AddVariable(identifier, type, "0");
            
            // Получаем значение 
            var ternaryExpressionGen = new TernaryExpressionGenerator();
            currentCode = ternaryExpressionGen.GenerateCodeForContext(varDefCtx.initializer().ternaryExpression(), currentCode);
            var resultValueRegister = currentCode.LastAssignedRegister;
            
            // Присваиваем и чистим регистр
            currentCode.AddRegisterToVariableWriting(identifier, type, resultValueRegister);
            currentCode.FreeRegister(resultValueRegister);
            
            return currentCode;
        }
        
    }
    
}