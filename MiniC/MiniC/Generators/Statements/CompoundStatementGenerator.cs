using Antlr4.Runtime;
using MiniC.Generators.Declarations;
using MiniC.Generators.Definitions;

namespace MiniC.Generators.Statements
{
    public class CompoundStatementGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var compoundStmtCtx = context as MiniCParser.CompoundStatementContext;
            var varDeclarations = compoundStmtCtx.varDeclaration();
            var varDefinitions = compoundStmtCtx.varDefinition();
            var statements = compoundStmtCtx.statement();
            
            // Добавляем в стек новый текущий скоуп
            var curScope = currentCode.PushScope(compoundStmtCtx);
            
            // Variable declarations
            if (varDeclarations != null)
            {
                var declarationGen = new VariableDeclarationCodeGenerator();
                foreach (var declaration in varDeclarations)
                    currentCode = declarationGen.GenerateCodeForContext(declaration, currentCode);
            }
            
            // Variable definitions
            if (varDefinitions != null)
            {
                var definitionGen = new VariableDefinitionCodeGenerator();
                foreach (var definition in varDefinitions)
                    currentCode = definitionGen.GenerateCodeForContext(definition, currentCode);
            }
            
            // Statements
            if (statements != null)
            {
                var statementGen = new StatementGenerator();
                foreach (var statement in statements)
                    currentCode = statementGen.GenerateCodeForContext(statement, currentCode);
            }
            
            // Удаление текущего скоупа из стека
            currentCode.PopScope();
            
            return currentCode;
        }
    }
}