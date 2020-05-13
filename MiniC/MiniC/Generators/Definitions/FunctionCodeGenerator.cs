using System;
using Antlr4.Runtime;
using MiniC.Generators.Statements;
using MiniC.Scopes;

namespace MiniC.Generators.Definitions
{
    public class FunctionCodeGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var functionDefinition = context as MiniCParser.FunctionDefinitionContext;
            var identifier = functionDefinition.functionHeader().Identifier().GetText();

            // Проставляем метку начала функции
            var funcName = identifier;
            currentCode.AddFunctionStart(funcName);

            // Пролог функции
            var funcSymbol = currentCode.GlobalScope.GetSymbol(identifier) as FunctionSymbol;
            var table = funcSymbol.Table;
            currentCode.AddComment("Prologue");

            Register firstParamAddressRegister = null;
            // Если в функции есть параметры, то получаем регистр с адресом первого (самый правый адрес в прежнем стеке)
            if (table.Count != 0)
            {
                var r0 = currentCode.AvaliableRegisters[0]; 
                r0.IsFree = false;
                var r1 = currentCode.AvaliableRegisters[1];
                r1.IsFree = false;
                firstParamAddressRegister = currentCode.GetFreeRegister();
                currentCode.AddAddingRegisterToRegister(firstParamAddressRegister, r0, r1);
                currentCode.AddInlineComment("First parameter address (topright)");
            }
            currentCode.AddAllocateStackFrame4000();
            var funcScope = currentCode.scopes.Get(functionDefinition.compoundStatement());
            readParametersToStack(currentCode, funcSymbol, firstParamAddressRegister, funcScope);

            currentCode.AddComment("End of prologue");
            currentCode.AvaliableRegisters[0].IsFree = true;
            currentCode.AvaliableRegisters[1].IsFree = true;

            // Тело функции
            var coumpoundGen = new CompoundStatementGenerator();
            currentCode = coumpoundGen.GenerateCodeForContext(functionDefinition.compoundStatement(), currentCode);
            
            // Проставляем метку конца функции
            currentCode.AddFunctionEnd(funcName);
            return currentCode;
        }

        private void readParametersToStack(AsmCodeWriter currentCode, FunctionSymbol funcSymbol,  
            Register firstParamAddressRegister, Scope compoundScope)
        {
            var funcTable = funcSymbol.Table;
            if (funcTable.Count == 0)
                return;

            var subValue = 0;
            var subAddressRegister = currentCode.GetFreeRegister();
            currentCode.AddValueToRegisterAssign(subAddressRegister, subValue.ToString(), SymbolType.GetType("int"));
            currentCode.AddInlineComment("Register for holding subtraction value");
            foreach (var symKeyVal in funcTable)
            {
                var symName = symKeyVal.Key;
                var sym = symKeyVal.Value;

                subValue = readSymbolToStack(currentCode, compoundScope.Parent, sym, firstParamAddressRegister, 
                    subAddressRegister, subValue);
            }
            
            currentCode.FreeRegister(subAddressRegister);
            currentCode.FreeRegister(firstParamAddressRegister);
        }

        private int readSymbolToStack(AsmCodeWriter currentCode, Scope funcScope, ISymbol symbol, 
            Register firstParamAddressRegister, Register subAddressRegister, int subValue, bool recursive = false)
        {
            var symbolType = symbol.Type;
            // Добавляем базовый адрес в таблицу символов (только если не рекурсивно зашли)
            if (!recursive)
            {
                var varSymbol = funcScope.GetSymbol(symbol.Name);
                varSymbol.BaseAddress = subValue.ToString();
                varSymbol.IsGlobal = false;
            }

            // Если массив или значение, то просто копируем в стек
            if (symbolType.IsArray || !symbolType.IsStructType())
            {
                currentCode.AddComment($"Pushing symbol {symbol.Name}");
                
                // Получаем адрес значения
                var varType = symbolType.IsArray ? SymbolType.GetType("int") : symbolType;    // size(int) == 4
                subValue += varType.Size;
                currentCode.AddValueToRegisterAssign(subAddressRegister, subValue.ToString(), varType);
                var addressRegister = currentCode.GetFreeRegister();
                currentCode.AddSubRegisterFromRegister(addressRegister, firstParamAddressRegister, 
                    subAddressRegister);

                // Получаем значение
                var valueRegister = currentCode.GetFreeRegister();
                currentCode.AddMemToRegisterReading(addressRegister, varType, valueRegister);
                
                // Записываем его в стек
                currentCode.PushRegisterValueToStack(valueRegister, varType.Size);

                // Чистим регистры
                currentCode.FreeRegister(valueRegister);
                currentCode.FreeRegister(addressRegister);

                return subValue;
            }

            // Если структрура, то проходимся по всем параметрам структуры 
            var structSymbol = currentCode.GlobalScope.FindStruct(symbolType);
            var structTable = structSymbol.Table;
            currentCode.AddComment($"Pushing struct {structSymbol.Name} {symbol.Name}");
            foreach (var symKeyVal in structTable)
            {
                var symName = symKeyVal.Key;
                var sym = symKeyVal.Value;

                subValue = readSymbolToStack(currentCode, funcScope, sym, firstParamAddressRegister, subAddressRegister, 
                    subValue, true);
            }

            return subValue;
        }
    }
}