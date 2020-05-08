using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using TestANTLR.Generators.Definitions;

namespace TestANTLR.Generators
{
    public class CompilationUnitCodeGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var compilationContext = context as MiniCParser.CompilationUnitContext;
            var translationUnitContext = compilationContext.translationUnit();
            var externalDeclarations = translationUnitContext.externalDeclaration();

            foreach (var externalDeclarationContext in externalDeclarations)
            {
                var definition = externalDeclarationContext.definition();
                var declaration = externalDeclarationContext.declaration();
                // Definitions
                if (definition != null)
                {
                    // Variable definition
                    var varDefinition = definition.varDefinition();
                    if (varDefinition != null)
                    {
                        var varDefGen = new VariableDefinitionCodeGenerator();
                        currentCode = varDefGen.GenerateCodeForContext(varDefinition, currentCode);
                        continue;
                    }
                    // Function definition
                    var funcDefinition = definition.functionDefinition();
                    if (funcDefinition != null)
                    {
                        var funcGenerator = new FunctionCodeGenerator();
                        currentCode = funcGenerator.GenerateCodeForContext(funcDefinition, currentCode);
                    }
                }
                // Declarations
                else
                {
                    // Variable declaration
                    var varDeclaration = declaration.varDeclaration();
                    if (varDeclaration != null)
                    {
                        var varDeclGen = new VariableDeclarationCodeGenerator();
                        currentCode = varDeclGen.GenerateCodeForContext(varDeclaration, currentCode);
                        continue;
                    }
                    // Function declaration (not used)
                    var funcDeclaration = declaration.functionDeclaration();
                    if (funcDeclaration != null)
                        continue;
                    // Struct declaration
                    var structDeclaration = declaration.structDeclaration();
                    if (structDeclaration != null)
                    {
                        // TODO: STRUCT DECLARATION HERE
                    }
                }
            }
            return currentCode;
        }
    }
}