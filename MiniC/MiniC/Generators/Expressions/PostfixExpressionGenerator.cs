using System.Linq;
using Antlr4.Runtime;
using MiniC.Scopes;

namespace MiniC.Generators.Expressions
{
    public class PostfixExpressionGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            // Primary expr only
            if (context is MiniCParser.PrimaryExpContext primaryExpression)
            {
                var primaryExprGen = new PrimaryExpressionGenerator();
                currentCode = primaryExprGen.GenerateCodeForContext(primaryExpression, currentCode);
            }
            // Indexing (a[i])
            else if (context is MiniCParser.ArrayReadContext arrayReadContext)
            {
                currentCode.AddComment("Getting braces value");
                
                // Вычисление значения в скобках
                var ternaryExpression = arrayReadContext.ternaryExpression();
                var ternaryExpressionGen = new TernaryExpressionGenerator();
                currentCode = ternaryExpressionGen.GenerateCodeForContext(ternaryExpression, currentCode);
                var inBracesValueRegister = getValueFromExpression(currentCode);
                
                // Привод типа в скобках, если надо
                convertTypeIfNeeded(currentCode, inBracesValueRegister, ternaryExpression);

                // Получение адреса переменной
                var postfixExpression = arrayReadContext.postfixExpression();
                var postfixExpressionGen = new PostfixExpressionGenerator();
                currentCode = postfixExpressionGen.GenerateCodeForContext(postfixExpression, currentCode);
                var variableAddressRegister = currentCode.LastReferencedAddressRegister;
                var variableType = variableAddressRegister.Type;
                
                // Получаем адрес нулевого элемента (то есть читаем значение текущего регистра) если массив не глобальный
                if (currentCode.GlobalScope.GetSymbol(currentCode.LastReferencedSymbol.Name) == null)
                    currentCode.AddMemToRegisterReading(variableAddressRegister, SymbolType.GetType("int"), 
                        variableAddressRegister);

                // Вычисление смещения
                currentCode.AddComment("Getting indexed value");
                var intType = SymbolType.GetType("int");
                var varSizeRegister = currentCode.GetFreeRegister();
                currentCode.AddValueToRegisterAssign(varSizeRegister, variableType.Size.ToString(), intType);
                var offsetRegister = currentCode.GetFreeRegister();
                currentCode.AddRegisterMpyRegister(offsetRegister, inBracesValueRegister, varSizeRegister);

                // Получение значения по индексу
                currentCode.AddAddingRegisterToRegister(variableAddressRegister, variableAddressRegister, 
                    offsetRegister);
                
                // Освобождение регистров
                currentCode.FreeRegister(offsetRegister);
                currentCode.FreeRegister(varSizeRegister);
                currentCode.FreeRegister(inBracesValueRegister);
            }
            // Function call (f(*))
            else if (context is MiniCParser.FunctionCallContext functionCallContext)
            {
                var identifier = functionCallContext.Identifier();
                var funcSymbol = currentCode.GlobalScope.GetSymbol(identifier.GetText()) as FunctionSymbol;
                
                // Writing parameters to stack
                currentCode.FuncParametersOffsetFromStackHead = writeParamsToStack(currentCode, functionCallContext,
                    funcSymbol, 0);

                // r0 и r1 нужны для записи текущей векхушки стека и указателя на первый параметр функции
                var r0 = currentCode.AvaliableRegisters[0];
                r0.IsFree = false;
                currentCode.AddAddingValueToRegister(r0, Register.SP(), currentCode.GetCurrentStackOffset());
                currentCode.AddInlineComment("Current stack head");
                var r1 = currentCode.AvaliableRegisters[1];
                r1.IsFree = false;
                currentCode.AddValueToRegisterAssign(r1, currentCode.FuncParametersOffsetFromStackHead.ToString(), 
                    SymbolType.GetType("int"));
                currentCode.AddInlineComment("Offset from stack head for first parameter");

                // Calling function
                currentCode.AddCall(identifier.GetText());
                
                // Returning value if something returned
                if (funcSymbol.Type.Name != "void")
                {
                    currentCode.AvaliableRegisters[0].IsFree = false;
                    currentCode.LastAssignedRegister = currentCode.AvaliableRegisters[0];
                    currentCode.FreeLastReferencedAddressRegister();
                }
            }
            // Dotting (a.b)
            else if (context is MiniCParser.StructReadContext structReadContext)
            {
                // Вычисление lvalue
                var postfixExpression = structReadContext.postfixExpression();
                var postfixEpressionGen = new PostfixExpressionGenerator();
                currentCode = postfixEpressionGen.GenerateCodeForContext(postfixExpression, currentCode);
                var lValueAddressRegister = currentCode.LastReferencedAddressRegister;
                var lValueType = lValueAddressRegister.Type;
                var stuctType = currentCode.LastReferencedStructType;
                
                // Вычисляем offset для переменной структуры
                var structSymbol = currentCode.GlobalScope.FindStruct(stuctType);
                var structOffset = structSymbol.VariableOffsetFromStartAddress(structReadContext.Identifier().GetText());
                
                // Запись offset в регистр
                var intType = SymbolType.GetType("int");
                var offsetRegister = currentCode.GetFreeRegister();
                currentCode.AddValueToRegisterAssign(offsetRegister, structOffset.ToString(), intType);
                
                // Получаем адрес нужной переменной в структуре
                currentCode.AddAddingRegisterToRegister(lValueAddressRegister, lValueAddressRegister, offsetRegister);
                currentCode.LastReferencedAddressRegister.Type = structSymbol.VariableType(structReadContext.Identifier().GetText());
                
                // Чистка регистров
                currentCode.FreeRegister(offsetRegister);
            }
            
            return currentCode;
        }

        private int writeParamsToStack(AsmCodeWriter currentCode, MiniCParser.FunctionCallContext context, 
            FunctionSymbol funcSymbol, int currentOffsetFromStackHead)
        {
            var table = funcSymbol.Table;
            var parametersList = context.parameterList();
            if (table.Count == 0)
                return 0;

            var currentTernaryExpression = parametersList.ternaryExpression();
            foreach (var symKeyValue in table.Reverse())
            {
                var symName = symKeyValue.Key;
                var sym = symKeyValue.Value;
                
                // Получаем значение, которое нужно передать
                var ternaryGen = new TernaryExpressionGenerator();
                currentCode = ternaryGen.GenerateCodeForContext(currentTernaryExpression, currentCode);

                // Записываем его в стек
                currentOffsetFromStackHead = writeSymbolToStack(currentCode, sym, currentTernaryExpression,
                    currentOffsetFromStackHead);

                // Переход к следующему параметру
                parametersList = parametersList.parameterList();
                currentTernaryExpression = parametersList?.ternaryExpression();
            }

            return currentOffsetFromStackHead;
        }

        private int writeSymbolToStack(AsmCodeWriter currentCode, ISymbol symbol, 
            MiniCParser.TernaryExpressionContext currentTernaryExpression, int offsetFromStackHead, 
            int offsetFromAddressRegister = 0)
        {
            // Если массив, то записываем адрес
            if (symbol.Type.IsArray)
            {
                // Вычисляем реальный адрес начала массива
                var addressRegister = currentCode.LastReferencedAddressRegister;
                var offsetFromAddress = currentCode.GetFreeRegister();
                currentCode.AddValueToRegisterAssign(offsetFromAddress, offsetFromAddressRegister.ToString(), 
                    SymbolType.GetType("int"));
                currentCode.AddAddingRegisterToRegister(addressRegister, addressRegister, offsetFromAddress);
                currentCode.FreeRegister(offsetFromAddress);
                
                // Считаем адрес в стеке для записи и записываем адрес 0 элемента в регистр, если массив не глобальный
                var offsetForVar = currentCode.GetCurrentStackOffset() + offsetFromStackHead;
                var zeroAddressRegister = currentCode.GetFreeRegister();
                if (currentCode.GlobalScope.GetSymbol(currentCode.LastReferencedSymbol.Name) == null)
                    currentCode.AddMemToRegisterReading(addressRegister, SymbolType.GetType("int"),
                    zeroAddressRegister);
                else 
                    currentCode.AddRegisterToRegisterAssign(zeroAddressRegister, addressRegister);
                currentCode.AddInlineComment("First array element address");

                // Записываем в стек
                currentCode.AddRegisterToMemWriting(Register.SP(), zeroAddressRegister, 
                    offsetForVar.ToString());
                
                // Чистим регистры и увеличиваем offset от головы стека
                currentCode.FreeRegister(zeroAddressRegister);
                currentCode.FreeLastReferencedAddressRegister();
                offsetFromStackHead += symbol.Type.IsArray ? 4 : symbol.Type.Size;
                return offsetFromStackHead;
            }
            
            // Если не структура
            if (!symbol.Type.IsStructType())
            {
                Register valueRegister = null;
                // Если константа
                if (currentCode.LastReferencedAddressRegister == null)
                    valueRegister = getValueFromExpression(currentCode);
                // Если переменная
                else
                {
                    // Вычисляем реальный адрес значения
                    var addressRegister = currentCode.LastReferencedAddressRegister;
                    var offsetFromAddress = currentCode.GetFreeRegister();
                    currentCode.AddValueToRegisterAssign(offsetFromAddress, offsetFromAddressRegister.ToString(), 
                        SymbolType.GetType("int"));
                    currentCode.AddAddingRegisterToRegister(addressRegister, addressRegister, offsetFromAddress);

                    // Получаем реальное значение
                    valueRegister = getValueFromExpression(currentCode,false);
                    
                    // Чистим регистр
                    currentCode.FreeRegister(offsetFromAddress);
                }
                
                // Приводим тип если нужно
                convertTypeIfNeeded(currentCode, valueRegister, currentTernaryExpression);  

                // Считаем адрес в стеке для записи
                var offsetForVar = currentCode.GetCurrentStackOffset() + offsetFromStackHead;
                    
                // Записываем в стек
                currentCode.AddRegisterToMemWriting(Register.SP(), valueRegister, 
                    offsetForVar.ToString());
                
                // Чистим регистры и увеличиваем offset от головы стека
                currentCode.FreeRegister(valueRegister);
                offsetFromStackHead += symbol.Type.IsArray ? 4 : symbol.Type.Size;
                return offsetFromStackHead;
            }
            
            // Если структура
            var structSymbol = currentCode.GlobalScope.FindStruct(symbol.Type);
            var structTable = structSymbol.Table;
            foreach (var symKeyVal in structTable.Reverse())
            {
                var symName = symKeyVal.Key;
                var sym = symKeyVal.Value;

                var offsetFromBase = structSymbol.VariableOffsetFromStartAddress(symName) + offsetFromAddressRegister;
                offsetFromStackHead = writeSymbolToStack(currentCode, sym, currentTernaryExpression, offsetFromStackHead,
                    offsetFromBase);
            }
            currentCode.FreeLastReferencedAddressRegister();

            return offsetFromStackHead;
        }
    }
}