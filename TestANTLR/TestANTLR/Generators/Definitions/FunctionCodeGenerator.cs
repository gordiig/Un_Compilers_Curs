using Antlr4.Runtime;
using TestANTLR.Generators.Statements;

namespace TestANTLR.Generators.Definitions
{
    public class FunctionCodeGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var functionDefinition = context as MiniCParser.FunctionDefinitionContext;
            
            // Проставляем метку начала функции
            var funcName = functionDefinition.functionHeader().Identifier().GetText();
            currentCode.AddFunctionStart(funcName);
            
            // Тело функции
            var coumpoundGen = new CompoundStatementGenerator();
            currentCode = coumpoundGen.GenerateCodeForContext(functionDefinition.compoundStatement(), currentCode);
            
            // Проставляем метку конца функции
            currentCode.AddFunctionEnd(funcName);
            return currentCode;
        }
    }
}