using System.Linq;
using Antlr4.Runtime;
using MiniC.Generators.Expressions;
using MiniC.Scopes;

namespace MiniC.Generators.Definitions
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
            // currentCode.AddComment($"Adding empty variable {symbol}");
            if (currentScope.IsGlobal())
                currentCode.AddGlobalVariable(symbol);
            else
            {
                currentCode.AddComment($"Variable {symbol.Name} declaration");
                currentCode.AddEmptyLocalVariable(symbol);
            }

            // Получаем значение, если переменная не структура (на структуры стоит запрет по семантике)
            var initializer = varDefCtx.initializer();
            if (type.IsArray)
            {
                currentCode.AddComment($"Setting values for array {symbol.Name} from right to left");
                var initList = initializer.initializerList();
                var currentInitializer = initList.initializer();
                var intType = SymbolType.GetType("int");
                for (int i = symbol.ArraySize-1; i >= 0; i--)
                {
                    // Получаем значение
                    var ternaryExprGen = new TernaryExpressionGenerator();
                    currentCode = ternaryExprGen.GenerateCodeForContext(currentInitializer.ternaryExpression(), currentCode);
                    var valueRegister = getValueFromExpression(currentCode);
                    
                    // Приводим тип если нужно
                    var valueTypeToConvert = currentCode.Conversions.Get(currentInitializer);
                    if (valueTypeToConvert != null)
                        currentCode.ConvertRegisterToType(valueRegister, valueRegister, 
                            valueTypeToConvert);
                    
                    // Кладем в регистр offset и присваиваем
                    var offsetForFirstValue = currentCode.GlobalScope.GetSymbol(identifier) == null ? 4 : 0;
                    var varOffset = i * type.Size + offsetForFirstValue;
                    currentCode.AddRegisterToVariableWritingWithOffset(symbol, valueRegister, varOffset.ToString());
                    currentCode.AddInlineComment($"Assigned {symbol.Name}[{i}]");
                    
                    // Чистим регистры и переходим к вычислению следующего значения
                    currentCode.FreeRegister(valueRegister);
                    initList = initList.initializerList();
                    currentInitializer = initList?.initializer();
                }
                
            }
            else
            {
                currentCode.AddComment($"Setting value for variable {symbol.Name}");
                var ternaryExpressionGen = new TernaryExpressionGenerator();
                currentCode = ternaryExpressionGen.GenerateCodeForContext(initializer.ternaryExpression(), currentCode);
                var resultValueRegister = getValueFromExpression(currentCode);
                
                // Приводим тип если нужно
                var valueTypeToConvert = currentCode.Conversions.Get(initializer);
                if (valueTypeToConvert != null)
                    currentCode.ConvertRegisterToType(resultValueRegister, resultValueRegister, 
                        valueTypeToConvert);
        
                // Присваиваем и чистим регистр
                currentCode.AddRegisterToVariableWriting(symbol, resultValueRegister);
                currentCode.FreeRegister(resultValueRegister);   
            }

            return currentCode;
        }
        
    }
    
}