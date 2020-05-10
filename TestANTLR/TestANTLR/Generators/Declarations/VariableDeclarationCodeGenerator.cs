using Antlr4.Runtime;
using TestANTLR.Scopes;

namespace TestANTLR.Generators
{
    public class VariableDeclarationCodeGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var varDeclarationContext = context as MiniCParser.VarDeclarationContext;
            var header = varDeclarationContext.varHeader();
            var type = SymbolType.GetType(header.typeSpecifier().GetText());
            var identifier = header.Identifier().GetText();
            currentCode.AddVariable(identifier, type);
            return currentCode;
        }
    }
}