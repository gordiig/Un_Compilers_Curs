using System.Linq;
using Antlr4.Runtime;
using TestANTLR.Generators.Expressions;
using TestANTLR.Scopes;

namespace TestANTLR.Generators.Definitions
{
    public class VariableDefinitionCodeGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var varDefCtx = context as MiniCParser.VarDefinitionContext;
            
            // Получаем инфу о переменной
            var header = varDefCtx.varHeader();
            var identifier = header.Identifier().GetText();
            var currentScope = currentCode.GetCurrentScope();
            var symbol = currentScope.GetSymbol(identifier) as VarSymbol;
            var type = symbol.Type;
            
            // Декларируем переменную со значением 0 (глобальную или нет)
            currentCode.AddComment($"Adding empty variable {symbol}");
            if (currentScope.IsGlobal())
                currentCode.AddGlobalVariable(symbol);
            else
                currentCode.AddEmptyLocalVariable(symbol);
            
            // Получаем значение, если переменная не структура (на структуры стоит запрет по семантике)
            var initializer = varDefCtx.initializer();
            if (type.IsArray)
            {
                currentCode.AddComment($"Setting values for array {symbol} from right to left");
                var initList = initializer.initializerList();
                var currentInitializer = initList.initializer();
                var intType = SymbolType.GetType("int");
                for (int i = symbol.ArraySize; i >= 0; i--)
                {
                    // Получаем значение
                    var ternaryExprGen = new TernaryExpressionGenerator();
                    currentCode = ternaryExprGen.GenerateCodeForContext(currentInitializer.ternaryExpression(), currentCode);
                    var valueRegister = currentCode.LastAssignedRegister;
                    
                    // Кладем в регистр offset и присваиваем
                    var varOffset = i * type.Size;
                    var offsetRegister = currentCode.GetFreeRegister();
                    currentCode.AddValueToRegisterAssign(offsetRegister, varOffset.ToString(), intType);
                    currentCode.AddRegisterToVariableWritingWithOffset(symbol, valueRegister, offsetRegister);
                    currentCode.AddInlineComment($"Assigned {symbol}[{i}]");
                    
                    // Чистим регистры и переходим к вычислению следующего значения
                    currentCode.FreeRegister(offsetRegister);
                    currentCode.FreeRegister(valueRegister);
                    initList = initList.initializerList();
                    currentInitializer = initList.initializer();
                }
                
            }
            else
            {
                currentCode.AddComment($"Setting value for variable {symbol}");
                var ternaryExpressionGen = new TernaryExpressionGenerator();
                currentCode = ternaryExpressionGen.GenerateCodeForContext(initializer.ternaryExpression(), currentCode);
                var resultValueRegister = currentCode.LastAssignedRegister;
        
                // Присваиваем и чистим регистр
                currentCode.AddRegisterToVariableWriting(symbol, resultValueRegister);
                currentCode.FreeRegister(resultValueRegister);   
            }

            return currentCode;
        }
        
    }
    
}