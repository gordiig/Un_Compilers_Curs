using Antlr4.Runtime;

namespace TestANTLR.Generators
{
    public interface ICodeGenerator
    {
        AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode);
    }
}