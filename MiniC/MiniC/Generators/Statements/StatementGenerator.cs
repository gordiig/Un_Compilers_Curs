using Antlr4.Runtime;

namespace MiniC.Generators.Statements
{
    public class StatementGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var statementCtx = context as MiniCParser.StatementContext;

            // Expression statement
            if (statementCtx.expressionStatement() != null)
            {
                var expressionStatementGen = new ExpressionStatementGenerator();
                currentCode = expressionStatementGen.GenerateCodeForContext(statementCtx.expressionStatement(), currentCode);
            }
            // Compound statement
            else if (statementCtx.compoundStatement() != null)
            {
                var compoundStatementGen = new CompoundStatementGenerator();
                currentCode = compoundStatementGen.GenerateCodeForContext(statementCtx.compoundStatement(), currentCode);
            }
            // If statement
            else if (statementCtx.ifStatement() != null)
            {
                var ifStatementGen = new IfStatementGenerator();
                currentCode = ifStatementGen.GenerateCodeForContext(statementCtx.ifStatement(), currentCode);
            }
            // Iteration statement
            else if (statementCtx.iterationStatement() != null)
            {
                var iterationStatementGen = new IterationStatementGenerator();
                currentCode = iterationStatementGen.GenerateCodeForContext(statementCtx.iterationStatement(), currentCode);
            }
            // Jump statement
            else
            {
                var jumpGen = new JumpStatementGenerator();
                currentCode = jumpGen.GenerateCodeForContext(statementCtx.jumpStatement(), currentCode);
            }

            return currentCode;
        }
    }
}