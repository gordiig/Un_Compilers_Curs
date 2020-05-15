using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using MiniC.Exceptions;
using MiniC.Scopes;

namespace MiniC.Generators
{
    public class AsmCodeWriter
    {
        private string _variables = "";
        private string _code = "";
        
        public string Code { get => _code; }
        public string Variables { get => _variables; }
        public string AllCode { get => "\t.section\t.data" + Variables + "\n\n\t.section\t.text\n" + Code + "\n"; }

        public ParseTreeProperty<SymbolType> Conversions;

        public AsmCodeWriter(ParseTreeProperty<Scope> skopes, GlobalScope globalScope, ParseTreeProperty<SymbolType> conversions)
        {
            scopes = skopes;
            GlobalScope = globalScope;
            Conversions = conversions;
            AvaliableRegisters = new Register[29];
            for (int i = 0; i < AvaliableRegisters.Length; i++)
                AvaliableRegisters[i] = new Register($"r{i}");
            AvaliablePredicateRegisters = new Register[4];
            for (int i = 0; i < AvaliablePredicateRegisters.Length; i++)
                AvaliablePredicateRegisters[i] = new Register($"p{i}");
        }

        public SymbolType LastReferencedStructType = null;
        public ISymbol LastReferencedSymbol = null;
        
        #region Registers work
        
        public Register LastAssignedRegister;
        public Register LastReferencedAddressRegister;
        public Stack<string> LoopStack = new Stack<string>();
        public Stack<string> IfStack = new Stack<string>();
        public Register[] AvaliableRegisters;
        public Register[] AvaliablePredicateRegisters;

        public Register GetFreeRegister(int startIdx = 0)
        {
            for (int i = startIdx; i < AvaliableRegisters.Length; i++)
            {
                if (AvaliableRegisters[i].IsFree)
                {
                    AvaliableRegisters[i].IsFree = false;
                    return AvaliableRegisters[i];
                }
            }
            throw new ArgumentException("No free registers left");
        }

        public void FreeRegister(Register register)
        {
            register.IsFree = true;
        }

        public void FreeLastReferencedAddressRegister()
        {
            if (LastReferencedAddressRegister != null)
                LastReferencedAddressRegister.IsFree = true;
            LastReferencedAddressRegister = null;
        }

        public Register GetFreePredicateRegister()
        {
            for (int i = 0; i < AvaliablePredicateRegisters.Length; i++)
            {
                if (AvaliablePredicateRegisters[i].IsFree)
                {
                    AvaliablePredicateRegisters[i].IsFree = false;
                    return AvaliablePredicateRegisters[i];
                }
            }
            throw new ArgumentException("No free predicate registers left");
        }

        public void FreePredicateRegister(Register pRegister)
        {
            pRegister.IsFree = true;
        }
        
        #endregion

        #region Functions and stack offset stack

        private Stack<string> funcStack = new Stack<string>();
        private Stack<int> variablesOffsetStack = new Stack<int>();
        public int FuncParametersOffsetFromStackHead = 0;
        public GlobalScope GlobalScope { get; }

        public void PushFunc(string funcName)
        {
            funcStack.Push(funcName);
            variablesOffsetStack.Push(0);
        }

        public (string, int) PopFunc()
        {
            var poppedFunc = funcStack.Pop();
            var poppedOffset = variablesOffsetStack.Pop();
            return (poppedFunc, poppedOffset);
        }

        public string GetCurrentFunc()
        {
            return funcStack.Peek();
        }

        public int GetCurrentStackOffset()
        {
            return variablesOffsetStack.Peek();
        }

        public int PushRegisterValueToStack(Register register, int size = -1)
        {
            var type = register.Type;
            var currentStackOffset = variablesOffsetStack.Pop();
            AddRegisterToMemWriting(Register.SP(), register, currentStackOffset.ToString());
            currentStackOffset += size == -1 ? type.Size : size;
            variablesOffsetStack.Push(currentStackOffset);
            return currentStackOffset;
        }

        #endregion
        
        #region Scopes work

        public ParseTreeProperty<Scope> scopes { get; }
        private Stack<Scope> scopesStack = new Stack<Scope>();

        public Scope PushScope(ParserRuleContext ctx)
        {
            scopesStack.Push(scopes.Get(ctx));
            return scopesStack.Peek();
        }

        public Scope PopScope()
        {
            return scopesStack.Pop();
        }

        public Scope GetCurrentScope()
        {
            return scopesStack.Peek();
        }

        #endregion
        
        // Writing to file
        public void WriteToFile(string filename = "../../../generated.S")
        {
            using (var writer = new StreamWriter(File.Open(filename, FileMode.Create)))
            {
                writer.AutoFlush = true;
                writer.Write(AllCode);
            }
        }

        #region Adding global variables

        public void AddGlobalVariable(VarSymbol symbol)
        {
            symbol.IsGlobal = true;
            symbol.BaseAddress = symbol.Name;
            addGlobalVariableRecursive(symbol.Name, symbol);
        }

        private void addGlobalVariableRecursive(string name, ISymbol symbol)
        {
            var defaultTypes = new string[] {"char", "int", "float"};
            if (defaultTypes.Contains(symbol.Type.Name))
            {
                if (symbol.Type.IsArray)
                    addGlobalEmptyArray(name, symbol.Type, symbol.ArraySize);
                else
                    addGlobalVariable(name, symbol.Type);
            }
            else
            {
                _code += $"\n{name}:";
                var structSymbol = GlobalScope.FindStruct(symbol.Type);
                if (symbol.Type.IsArray)
                {
                    for (int i = 0; i < symbol.ArraySize; i++)
                    {
                        _code += $"\n{name}_{i}:";
                        foreach (var symKeyValue in structSymbol.Table)
                        {
                            var symName = symKeyValue.Key;
                            var sym = symKeyValue.Value;
                            addGlobalVariableRecursive($"{name}_{symbol.Type.Name}_{symName}_{i}", sym);
                        }   
                    }
                }
                else
                {
                    foreach (var symKeyValue in structSymbol.Table)
                    {
                        var symName = symKeyValue.Key;
                        var sym = symKeyValue.Value;
                        addGlobalVariableRecursive($"{name}_{symbol.Type.Name}_{symName}", sym);
                    }   
                }
            }
        }

        private void addGlobalVariable(string name, SymbolType type)
        {
            _variables += $"\n{name}:\n\t.{type.Name}\t0";
        }

        public void addGlobalEmptyArray(string name, SymbolType type, int capacity)
        {
            var header = $"\n{name}:\n\t.{type.Name} ";
            for (int i = 0; i < capacity; i++)
            {
                header += "0, ";
            }
            if (capacity > 0)
                header = header.Remove(header.Length - 2, 2);
            _variables += header;
        }

        #endregion

        #region Adding local variables

        public int AddEmptyLocalVariable(VarSymbol symbol)
        {
            var currentOffset = variablesOffsetStack.Pop();

            symbol.IsGlobal = false;
            symbol.BaseAddress = currentOffset.ToString();
            currentOffset = addLocalVariable(symbol, currentOffset);

            variablesOffsetStack.Push(currentOffset);
            return currentOffset;
        }

        private int addLocalVariable(ISymbol symbol, int currentOffset)
        {
            var defaultTypes = new string[] {"char", "int", "float"};
            // Если стандартный тип
            if (defaultTypes.Contains(symbol.Type.Name))
            {
                var memFunc = symbol.Type.MemFunc;
                // Если массив
                if (symbol.Type.IsArray)
                {
                    // Добавляем указатель на нулевой элемент (костыли ура вот указатели не делали теперь хлебаем)
                    var pointerRegister = GetFreeRegister();
                    AddAddingValueToRegister(pointerRegister, Register.SP(), currentOffset + 4);
                    _code += $"\n\tmemw(SP + #{currentOffset}) = {pointerRegister};";
                    FreeRegister(pointerRegister);
                    currentOffset += 4;
                    for (int i = 0; i < symbol.ArraySize; i++)
                    {
                        _code += $"\n\t{memFunc}(SP + #{currentOffset}) = #0;";
                        currentOffset += symbol.Type.Size;
                    }   
                }
                // Если одно значение
                else
                {
                    _code += $"\n\t{memFunc}(SP + #{currentOffset}) = #0;";
                    currentOffset += symbol.Type.Size;
                }
            }
            // Если структура
            else
            {
                var structSymbol = GlobalScope.FindStruct(symbol.Type);
                // Если массив
                if (symbol.Type.IsArray)
                {
                    // Добавляем указатель на нулевой элемент (костыли ура вот указатели не делали теперь хлебаем)
                    var pointerRegister = GetFreeRegister();
                    AddAddingValueToRegister(pointerRegister, Register.SP(), currentOffset + 4);
                    _code += $"\n\tmemw(SP + #{currentOffset}) = {pointerRegister};";
                    FreeRegister(pointerRegister);
                    currentOffset += 4;
                    for (int i = 0; i < symbol.ArraySize; i++)
                    {
                        foreach (var symKeyValue in structSymbol.Table)
                        {
                            var sym = symKeyValue.Value;
                            currentOffset = addLocalVariable(sym, currentOffset);
                        }
                    }
                }
                // Если одна структура
                else
                {
                    foreach (var symKeyValue in structSymbol.Table)
                    {
                        var sym = symKeyValue.Value;
                        currentOffset = addLocalVariable(sym, currentOffset);
                    }
                }
            }

            return currentOffset;
        }


        #endregion
        
        #region Adding function labels
        public void AddFunctionStart(string name)
        {
            var label = $"func_{name}_start";
            _code += $"\n{label}:";
            PushFunc(name);
        }

        public void AddFunctionEnd(string name)
        {
            var label = $"func_{name}_end";
            _code += $"\n{label}:";
            _code += $"\n\tdealloc_return;\n";
            PopFunc();
        }

        public void AddReturn(string funcName)
        {
            var label = $"func_{funcName}_end";
            AddJump(label);
        }

        public void AddReturnValue(Register sourceRegister)
        {
            AddRegisterToRegisterAssign(AvaliableRegisters[0], sourceRegister);
        }
        
        #endregion

        #region Allocating stack frame

        public void AddAllocateStackFrame4000()
        {
            _code += "\n\tallocframe(#4000)";
        }

        #endregion
        
        #region Adding loop labels
        public void AddLoopStart(string name)
        {
            var label = $"loop_{name}_start";
            _code += $"\n{label}:";
            LoopStack.Push(name);
        }

        public void AddLoopEnd(string name)
        {
            var label = $"loop_{name}_end";
            _code += $"\n{label}:";
            LoopStack.Pop();
        }

        public void AddContinue(string loopName)
        {
            var label = $"loop_{loopName}_start";
            AddJump(label);
        }

        public void AddConditionalContinue(string loopName, Register pRegister, bool negate = false)
        {
            var label = $"loop_{loopName}_start";
            AddConditionalJump(pRegister, label, negate);
        }

        public void AddBreak(string loopName)
        {
            var label = $"loop_{loopName}_end";
            AddJump(label);
        }

        public void AddConditionalBreak(string loopName, Register pRegister, bool negate = false)
        {
            var label = $"loop_{loopName}_end";
            AddConditionalJump(pRegister, label, negate);
        }
        
        #endregion

        #region Adding if labels
        public void AddIfStart(string name)
        {
            var label = $"if_{name}_start";
            _code += $"\n{label}:";
            IfStack.Push(name);
        }

        public void AddIfEnd(string name)
        {
            var label = $"if_{name}_end";
            _code += $"\n{label}:";
            IfStack.Pop();
        }

        public void AddIfElse(string ifName)
        {
            var label = $"if_{ifName}_else";
            _code += $"\n{label}:";
        }

        public void AddJumpToElse(string ifName)
        {
            var label = $"if_{ifName}_else";
            AddJump(label);
        }

        public void AddConditionalJumpToElse(string ifName, Register pRegister, bool negate = false)
        {
            var label = $"if_{ifName}_else";
            AddConditionalJump(pRegister, label, negate);
        }

        public void AddJumpToIfEnd(string ifName)
        {
            var label = $"if_{ifName}_end";
            AddJump(label);
        }

        public void AddConditionalJumpToIfEnd(string ifName, Register pRegister, bool negate = false)
        {
            var label = $"if_{ifName}_end";
            AddConditionalJump(pRegister, label, negate);
        }
        
        #endregion

        #region Read-write variables to register

        public void AddRegisterToVariableWriting(VarSymbol variable, Register register)
        {
            var memRegister = GetFreeRegister();
            var memFunc = variable.Type.MemFunc;
            if (memFunc.Length == 0)
                memFunc = SymbolType.GetType("int").MemFunc;
            if (variable.IsGlobal)
                _code += $"\n\t{memRegister} = ##{variable.BaseAddress};";
            else
                _code += $"\n\t{memRegister} = add(SP, #{variable.BaseAddress})";
            _code += $"\n\t{memFunc}({memRegister}) = {register};";
            // LastReferencedVariable = variable;
            FreeRegister(memRegister);
        }

        public void AddVariableAddressToRegisterReading(VarSymbol variable, Register register)
        {
            if (variable.IsGlobal)
                _code += $"\n\t{register} = ##{variable.BaseAddress};";
            else
                _code += $"\n\t{register} = add(SP, #{variable.BaseAddress})";
            register.Type = variable.Type;
            LastReferencedAddressRegister = register;
        }

        public void AddRegisterToVariableWritingWithOffset(VarSymbol variable, Register register, string offset)
        {
            var memRegister = GetFreeRegister();
            var memFunc = variable.Type.MemFunc;
            if (memFunc.Length == 0)
                memFunc = SymbolType.GetType("int").MemFunc;
            if (variable.IsGlobal)
                _code += $"\n\t{memRegister} = ##{variable.BaseAddress};";
            else
                _code += $"\n\t{memRegister} = add(SP, #{variable.BaseAddress});";
            _code += $"\n\t{memFunc}({memRegister} + #{offset}) = {register};";
            FreeRegister(memRegister);
        }

        public void AddMemToRegisterReading(Register addressRegister, SymbolType type, Register destRegister, string offsetValue = "")
        {
            var memFunc = type.MemFunc;
            if (memFunc == "")
                memFunc = "memw";
            var offsetSuffix = offsetValue == "" ? "" : $" + #{offsetValue}";
            _code += $"\n\t{destRegister} = {memFunc}({addressRegister}{offsetSuffix});";
            destRegister.Type = type;
            LastAssignedRegister = destRegister;
        }

        public void AddRegisterToMemWriting(Register addressRegister, Register sourceRegister, string offsetValue = "")
        {
            var memFunc = sourceRegister.Type.MemFunc;
            if (memFunc == "")
                memFunc = "memw";
            var offsetSuffix = offsetValue == "" ? "" : $" + #{offsetValue}";
            _code += $"\n\t{memFunc}({addressRegister}{offsetSuffix}) = {sourceRegister};";
        }

        public void AddWriteStackHeadAddressToRegister(Register sourceRegister)
        {
            var currentOffset = GetCurrentStackOffset();
            _code = $"\n\t{sourceRegister} = add(SP, #{currentOffset});";
            sourceRegister.Type = SymbolType.GetType("int");
        }
        
        #endregion
        
        #region Working with registers
        public void AddValueToRegisterAssign(Register register, string value, SymbolType type)
        {
            if (type.Name == "float")
            {
                // sfmake работает очень странно, так что будет так
                var floatHex = BitConverter.GetBytes(float.Parse(value));
                var floatHexAsInt = BitConverter.ToInt32(floatHex, 0);
                AddValueToRegisterAssign(register, floatHexAsInt.ToString(), SymbolType.GetType("int"));
                AddInlineComment($"{value} as hex (sfmake works poorly)");
            }
            else if (type.Name == "int")
            {
                // Переводим из 8/10/16-ричной СИ в 10
                var intValue = 0;
                if (value.ToLower().Contains("0x"))
                    intValue = Convert.ToInt32(value, 16);
                else if (value.First() == '0')
                    intValue = Convert.ToInt32(value, 8);
                else
                    intValue = Convert.ToInt32(value, 10);
                _code += $"\n\t{register} = #{intValue};";   
            }
            else  // char
            {
                // Удаляем ''
                var charValue = value.Remove(0, 1);
                charValue = charValue.Remove(charValue.Length-1);
                
                var realChar = '\0';
                if (charValue.Length == 1)
                    realChar = char.Parse(charValue);
                else if (charValue.Length == 2)
                {
                    switch (charValue[1])
                    {
                        case 'a':
                            realChar = '\a';
                            break;
                        case 'b':
                            realChar = '\b';
                            break;
                        case '\\':
                            realChar = '\\';
                            break;
                        case '\'':
                            realChar = '\'';
                            break;
                        case 't':
                            realChar = '\t';
                            break;
                        case 'n':
                            realChar = '\n';
                            break;
                        case 'r':
                            realChar = '\r';
                            break;
                        case '0':
                            realChar = '\0';
                            break;
                        default:
                            throw new CodeGenerationException($"Unknown terminator symbol {charValue}");
                    }
                }
                else 
                    throw new CodeGenerationException($"Invalid char literal {value}"); 
                var asciiCode = (int) realChar;
                _code += $"\n\t{register} = #{asciiCode};";
            }
            register.Type = type;
            LastAssignedRegister = register;
        }

        public void AddRegisterToRegisterAssign(Register lhs, Register rhs)
        {
            _code += $"\n\t{lhs} = {rhs};";
            lhs.Type = rhs.Type;
            LastAssignedRegister = lhs;
        }

        public void AddConditionalRegisterToRegisterAssign(Register pRegister, Register destRegister, 
            Register sourceRegisterIfTrue, Register sourceRegisterIfFalse)
        {
            _code += $"\n\tif({pRegister}) {destRegister} = {sourceRegisterIfTrue}";
            _code += $"\n\tif(!{pRegister}) {destRegister} = {sourceRegisterIfFalse}";
            destRegister.Type = sourceRegisterIfTrue.Type;    // у true и false одинаковые типы должны быть
            LastAssignedRegister = destRegister;
        }
        
        #endregion
        
        #region ALU function
        public void AddAddingRegisterToRegister(Register lhs, Register s1, Register s2)
        {
            var resultType = SymbolType.GetBigger(s1.Type, s2.Type);
            var addFunc = resultType.AddFunc;
            if (addFunc == "")
                addFunc = "add";
            _code += $"\n\t{lhs} = {addFunc}({s1}, {s2});";
            lhs.Type = resultType;
            LastAssignedRegister = lhs;
        }

        public void AddAddingValueToRegister(Register lhs, Register r1, int value)
        {
            _code += $"\n\t{lhs} = add({r1}, #{value});";
            lhs.Type = SymbolType.GetType("int");
            LastAssignedRegister = lhs;
        }
        
        public void AddSubRegisterFromRegister(Register lhs, Register s1, Register s2)
        {
            var resultType = SymbolType.GetBigger(s1.Type, s2.Type);
            var subFunc = resultType.SubFunc;
            if (subFunc == "")
                subFunc = "sub";
            _code += $"\n\t{lhs} = {subFunc}({s1}, {s2});";
            lhs.Type = resultType;
            LastAssignedRegister = lhs;
        }

        public void AddNegateRegister(Register destRegister, Register sourceRegister)
        {
            _code += $"\n\t{destRegister} = neg({sourceRegister});";
            destRegister.Type = sourceRegister.Type;
            LastAssignedRegister = destRegister;
        }

        public void AddRegisterMpyRegister(Register destRegister, Register r1, Register r2)
        {
            var resultType = SymbolType.GetBigger(r1.Type, r2.Type);
            var mpyFunc = resultType.MpyFunc;
            if (mpyFunc == "")
                mpyFunc = "mpyi";
            _code += $"\n\t{destRegister} = {mpyFunc}({r1}, {r2});";
            destRegister.Type = resultType;
            LastAssignedRegister = destRegister;
        }

        public void AddRegisterDivRegister(Register destRegister, Register r1, Register r2)
        {
            var type = SymbolType.GetBigger(r1.Type, r2.Type);
            if (type.Name == "float")
                divWithRegisterSaving(destRegister, r1, r2, 7, "__hexagon_divsf3");
            else 
                divWithRegisterSaving(destRegister, r1, r2, 5, "__hexagon_divsi3");
        }

        public void AddRegisterModRegister(Register destRegister, Register r1, Register r2)
        {
            divWithRegisterSaving(destRegister, r1, r2, 2, "__hexagon_umodsi3");
        } 

        private void divWithRegisterSaving(Register destRegister, Register r1, Register r2, int maxSavedRegisterIdx, 
            string divFunc)
        {
            // Регистры с 0 по maxSavedRegisterIdx участвуют в макросе деления, так что сохраняем их
            AddComment($"Saving r0-r{maxSavedRegisterIdx} before sfdiv");
            var savedRegisters = new List<Register>();
            for (int i = 0; i < maxSavedRegisterIdx+1; i++)
            {
                var r = AvaliableRegisters[i];
                var sr = GetFreeRegister(maxSavedRegisterIdx+1);
                AddRegisterToRegisterAssign(sr, r);
                savedRegisters.Add(sr);
            }

            // Кладем в регистры r0 и r1 значения для деления
            AddComment($"Calling {divFunc}");
            var reg0 = AvaliableRegisters[0];
            var reg1 = AvaliableRegisters[1];
            AddRegisterToRegisterAssign(reg0, r1);
            AddRegisterToRegisterAssign(reg1, r2);
            
            // Вызываем макрос
            _code += $"\n\tcall {divFunc};";
            
            // Результат деления сейчас в r0, поэтому восстанавливаем все регистры кроме нулевого
            AddComment($"Restoring r1-r{maxSavedRegisterIdx} registers");
            for (int i = 1; i < maxSavedRegisterIdx+1; i++)
            {
                var r = AvaliableRegisters[i];
                AddRegisterToRegisterAssign(r, savedRegisters[i]);
                FreeRegister(savedRegisters[i]);
            }
            
            // Копируем значение из r0 в первый свободный регистр и восстанавливаем r0
            if (destRegister.Name == "r0")
                AddComment($"Dest register set to r0, no need to restore its prev value, {divFunc} is over");
            else
            {
                AddComment("Copying result and restoring r0");
                AddRegisterToRegisterAssign(destRegister, reg0);
                AddRegisterToRegisterAssign(reg0, savedRegisters[0]);
            }
            FreeRegister(savedRegisters[0]);
            LastAssignedRegister = destRegister;
        }
        
        #endregion

        #region Converting types

        public void AddIntRegisterToFloatConvert(Register destRegister, Register sourceRegister)
        {
            if (sourceRegister.Type.Name == "float")
                return;
            _code += $"\n\t{destRegister} = convert_w2sf({sourceRegister});";
            destRegister.Type = SymbolType.GetType("float");
            LastAssignedRegister = destRegister;
        }

        public void AddFloatRegisterToIntConvert(Register destRegister, Register sourceRegister)
        {
            if (sourceRegister.Type.Name == "int" || sourceRegister.Type.Name == "char")
                return;
            _code += $"\n\t{destRegister} = convert_sf2w({sourceRegister});";
            destRegister.Type = SymbolType.GetType("int");
            LastAssignedRegister = destRegister;
        }

        public void ConvertRegisterToType(Register destRegister, Register sourceRegister, SymbolType type)
        {
            if (type.Name == "float")
                AddIntRegisterToFloatConvert(destRegister, sourceRegister);
            else if (type.Name == "char" || type.Name == "int")
                AddFloatRegisterToIntConvert(destRegister, sourceRegister);
            else 
                throw new CodeGenerationException($"Unknown type for conversion \"{type.Name}\"");
        }

        #endregion
        
        #region Bit manipulation functions
        public void AddRegisterAndRegister(Register destRegister, Register r1, Register r2)
        {
            var resultType = SymbolType.GetBigger(r1.Type, r2.Type);
            _code += $"\n\t{destRegister} = and({r1}, {r2});";
            destRegister.Type = resultType;
            LastAssignedRegister = destRegister;
        }

        public void AddRegisterOrRegister(Register destRegister, Register r1, Register r2)
        {
            var resultType = SymbolType.GetBigger(r1.Type, r2.Type);
            _code += $"\n\t{destRegister} = or({r1}, {r2});";
            destRegister.Type = resultType;
            LastAssignedRegister = destRegister;
        }

        public void AddRegisterXorRegister(Register destRegister, Register r1, Register r2)
        {
            var resultType = SymbolType.GetBigger(r1.Type, r2.Type);
            _code += $"\n\t{destRegister} = xor({r1}, {r2});";
            destRegister.Type = resultType;
            LastAssignedRegister = destRegister;
        }

        public void AddRegisterRightShiftRegister(Register destRegister, Register r1, Register r2)
        {
            var resultType = SymbolType.GetBigger(r1.Type, r2.Type);
            _code += $"\n\t{destRegister} = lsr({r1}, {r2});";
            destRegister.Type = resultType;
            LastAssignedRegister = destRegister;
        }

        public void AddRegisterLefShiftRegister(Register destRegister, Register r1, Register r2)
        {
            var resultType = SymbolType.GetBigger(r1.Type, r2.Type);
            _code += $"\n\t{destRegister} = lsl({r1}, {r2});";
            destRegister.Type = resultType;
            LastAssignedRegister = destRegister;
        }

        public void AddNotRegister(Register destRegister, Register sourceRegister)
        {
            _code += $"\n\t{destRegister} = not({sourceRegister});";
            destRegister.Type = sourceRegister.Type;
            LastAssignedRegister = destRegister;
        }
        
        #endregion
        
        #region Adding compares
        public void AddCompareRegisterEqRegister(Register pRegister, Register r1, Register r2, bool negate = false)
        {
            var type = SymbolType.GetBigger(r1.Type, r2.Type);
            if (type.Name == "float")
            {
                if (negate) 
                    throw new NotImplementedException("Negate for float!");
                _code += $"\n\t{pRegister} = sfcmp.eq({r1}, {r2});";
            }
            else
            {
                var negateSym = negate ? "!" : "";
                _code += $"\n\t{pRegister} = {negateSym}cmp.eq({r1}, {r2});";   
            }
        }

        public void AddCompareRegisterEqNumber(Register pRegister, Register register, string value, bool negate = false)
        {
            var type = register.Type;
            if (type.Name == "float")
            {
                if (negate)
                    throw new NotImplementedException("Negate for float!");
                var flRegister = GetFreeRegister();
                AddValueToRegisterAssign(flRegister, value, type);
                AddCompareRegisterEqRegister(pRegister, register, flRegister, negate);
                FreeRegister(flRegister);
            }
            else
            {
                var negateSym = negate ? "!" : "";
                _code += $"\n\t{pRegister} = {negateSym}cmp.eq({register}, #{value});";   
            }
        }

        public void AddCompareRegisterGeRegister(Register pRegister, Register r1, Register r2)
        {
            var type = SymbolType.GetBigger(r1.Type, r2.Type);
            var sfPrefix = type.Name == "float" ? "sf" : "";
            _code += $"\n\t{pRegister} = {sfPrefix}cmp.ge({r1}, {r2});";
        }
        
        public void AddCompareRegisterGtRegister(Register pRegister, Register r1, Register r2)
        {
            var type = SymbolType.GetBigger(r1.Type, r2.Type);
            var sfPrefix = type.Name == "float" ? "sf" : "";
            _code += $"\n\t{pRegister} = {sfPrefix}cmp.gt({r1}, {r2});";
        }
        
        public void AddCompareRegisterLeRegister(Register pRegister, Register r1, Register r2)
        {
            // LE делается через GE простым свапом параметров
            var type = SymbolType.GetBigger(r1.Type, r2.Type);
            var sfPrefix = type.Name == "float" ? "sf" : "";
            _code += $"\n\t{pRegister} = {sfPrefix}cmp.ge({r2}, {r1});";
        }
        
        public void AddCompareRegisterLtRegister(Register pRegister, Register r1, Register r2)
        {
            // LT делается через GT простым свапом параметров
            var type = SymbolType.GetBigger(r1.Type, r2.Type);
            var sfPrefix = type.Name == "float" ? "sf" : "";
            _code += $"\n\t{pRegister} = {sfPrefix}cmp.gt({r2}, {r1});";
        }
        
        #endregion
        
        #region Adding calls, jumps and returns
        public void AddJump(string label)
        {
            _code += $"\n\tjump {label};";
        }

        public void AddConditionalJump(Register pRegister, string label, bool negate = false)
        {
            var negationSym = negate ? "!" : "";
            _code += $"\nif({negationSym}{pRegister}) jump {label}";
        }

        public void AddJumpToRegister(Register register)
        {
            _code += $"\n\tjumpr {register};";
        }

        public void AddCall(string funcName)
        {
            _code += $"\n\tcall func_{funcName}_start;";
        }
        
        #endregion

        #region Adding comments
        public void AddInlineComment(string comment)
        {
            _code += $"\t// {comment}";
        }

        public void AddComment(string comment, bool withTab = true)
        {
            var tabSym = withTab ? "\t" : "";
            _code += $"\n{tabSym}// {comment}";
        }
        
        #endregion

        #region Adding plain code
        public void AddPlainCode(string code)
        {
            _code += $"\n{code}";
        }
        
        #endregion

    }
}