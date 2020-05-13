using Antlr4.Runtime;
using MiniC.Scopes;

namespace MiniC.Generators.Declarations
{
    public class VariableDeclarationCodeGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var varDeclarationContext = context as MiniCParser.VarDeclarationContext;
            
            // Получаем данные о переменной
            var header = varDeclarationContext.varHeader();
            var identifier = header.Identifier().GetText();
            var currentScope = currentCode.GetCurrentScope();
            var symbol = currentScope.GetSymbol(identifier) as VarSymbol;

            // Если текущий скоуп глобальный, то и добавляем в код как глобальную переменную;
            if (currentScope.IsGlobal())
                currentCode.AddGlobalVariable(symbol);
            else
            {
                currentCode.AddComment($"Variable {symbol.Name} declaration");
                currentCode.AddEmptyLocalVariable(symbol);
            }

            return currentCode;
        }
    }
}