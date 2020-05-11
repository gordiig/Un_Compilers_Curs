using System.Linq;
using Antlr4.Runtime;
using TestANTLR.Scopes;

namespace TestANTLR.Generators.Expressions
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
                var inBracesValueRegister = currentCode.LastAssignedRegister;
                
                // Привод типа в скобках, если надо
                convertTypeIfNeeded(currentCode, inBracesValueRegister, ternaryExpression);

                // Получение адреса переменной
                var postfixExpression = arrayReadContext.postfixExpression();
                var postfixExpressionGen = new PostfixExpressionGenerator();
                currentCode = postfixExpressionGen.GenerateCodeForContext(postfixExpression, currentCode);
                var variableAddressRegister = currentCode.LastReferencedAddressRegister;
                var variableType = variableAddressRegister.Type;
                
                // Получаем адрес нулевого элемента (то есть читаем значение текущего регистра)
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

                // Calling function
                currentCode.AddCall(identifier.GetText());
                
                // Returning value if something returned
                if (funcSymbol.Type.Name != "void")
                    currentCode.LastAssignedRegister = currentCode.AvaliableRegisters[0];
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
                
                // Вычисляем offset для переменной структуры
                var structSymbol = currentCode.GlobalScope.FindStruct(lValueType);
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
                currentOffsetFromStackHead = writeSymbolToStack(currentCode, sym, currentOffsetFromStackHead);

                // Переход к следующему параметру
                parametersList = parametersList.parameterList();
                currentTernaryExpression = parametersList?.ternaryExpression();
            }

            return currentOffsetFromStackHead;
        }

        private int writeSymbolToStack(AsmCodeWriter currentCode, ISymbol symbol, int offsetFromStackHead,
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
                
                // Считаем адрес в стеке для записи
                var offsetForVar = currentCode.GetCurrentStackOffset() + offsetFromStackHead;
                    
                // Записываем в стек
                currentCode.AddRegisterToMemWriting(Register.SP(), addressRegister, 
                    offsetForVar.ToString());
                
                // Чистим регистры и увеличиваем offset от головы стека
                currentCode.FreeLastReferencedAddressRegister();
                offsetFromStackHead += symbol.Type.IsArray ? 4 : symbol.Type.Size;
                return offsetFromStackHead;
            }
            
            // Если не структура
            if (!symbol.Type.IsStructType())
            {
                // Вычисляем реальный адрес начала массива
                var addressRegister = currentCode.LastReferencedAddressRegister;
                var offsetFromAddress = currentCode.GetFreeRegister();
                currentCode.AddValueToRegisterAssign(offsetFromAddress, offsetFromAddressRegister.ToString(), 
                    SymbolType.GetType("int"));
                currentCode.AddAddingRegisterToRegister(addressRegister, addressRegister, offsetFromAddress);
                currentCode.FreeRegister(offsetFromAddress);
                
                // Получаем значение для передачи, и кладем его в регистр
                var valueRegister = getValueFromExpression(currentCode);
                
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
                offsetFromStackHead += writeSymbolToStack(currentCode, sym, offsetFromStackHead, offsetFromBase);
            }

            return offsetFromStackHead;
        }
    }
}