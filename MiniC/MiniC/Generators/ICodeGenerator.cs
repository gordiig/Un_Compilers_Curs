using Antlr4.Runtime;

namespace MiniC.Generators
{
    public interface ICodeGenerator
    {
        AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode);
    }
}