using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using MiniC.Generators.Declarations;
using MiniC.Generators.Definitions;

namespace MiniC.Generators
{
    public class CompilationUnitCodeGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var compilationContext = context as MiniCParser.CompilationUnitContext;
            var translationUnitContext = compilationContext.translationUnit();
            var externalDeclarations = translationUnitContext.externalDeclaration();
            
            // Добавляем глобальный скоуп в стек
            var globalScope = currentCode.PushScope(compilationContext);

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
                    // Struct declaration
                    var structDeclaration = declaration.structDeclaration();
                    if (structDeclaration != null)
                    {
                        // TODO: STRUCT DECLARATION HERE
                    }
                }
            }
            
            // Удаляем глобальный скоуп
            currentCode.PopScope();
            
            return currentCode;
        }
    }
}