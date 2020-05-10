using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using TestANTLR.Scopes;

namespace TestANTLR.Generators
{
    public class AsmCodeWriter
    {
        private string _variables = "";
        private string _code = "";
        
        public string Code { get => _code; }
        public string Variables { get => _variables; }
        public string AllCode { get => "\t.section\t.data" + Variables + "\n\n\t.section\t.text\n" + Code + "\n"; }

        public AsmCodeWriter(ParseTreeProperty<Scope> skopes, GlobalScope globalScope)
        {
            scopes = skopes;
            GlobalScope = globalScope;
            AvaliableRegisters = new bool[27];
            for (int i = 0; i < AvaliableRegisters.Length; i++)
                AvaliableRegisters[i] = true;
        }
        
        #region Registers work
        
        public string LastAssignedRegister = "r0";
        public string LastReferencedAddressRegister = "";
        public SymbolType LastReferencedAddressRegisterType = SymbolType.GetType("int");
        public Stack<string> LoopStack = new Stack<string>();
        public Stack<string> IfStack = new Stack<string>();
        public bool[] AvaliableRegisters;
        public bool[] AvaliablePredicateRegisters = new []{ true, true, true, true };

        public string GetFreeRegister()
        {
            for (int i = 0; i < AvaliableRegisters.Length; i++)
            {
                if (AvaliableRegisters[i])
                {
                    var ans = $"r{i}";
                    AvaliableRegisters[i] = false;
                    return ans;
                }
            }
            throw new ArgumentException("No free registers left");
        }

        public void FreeRegister(string register)
        {
            var intStr = register.Remove(0, 1);
            var idx = int.Parse(intStr);
            AvaliableRegisters[idx] = true;
        }

        public void FreeLastReferencedAddressRegister()
        {
            if (LastReferencedAddressRegister.Length != 0)
                FreeRegister(LastReferencedAddressRegister);
            LastReferencedAddressRegister = "";
        }

        public void FreeRegisters(string[] registers)
        {
            foreach (var register in registers)
            {
                FreeRegister(register);
            }
        }

        public string GetFreePredicateRegister()
        {
            for (int i = 0; i < AvaliablePredicateRegisters.Length; i++)
            {
                if (AvaliablePredicateRegisters[i])
                {
                    var ans = $"p{i}";
                    AvaliablePredicateRegisters[i] = false;
                    return ans;
                }
            }
            throw new ArgumentException("No free predicate registers left");
        }

        public void FreePredicateRegister(string pRegister)
        {
            var intStr = pRegister.Remove(0, 1);
            var idx = int.Parse(intStr);
            AvaliablePredicateRegisters[idx] = true;
        }
        
        #endregion

        #region Functions and stack offset stack

        private Stack<string> funcStack = new Stack<string>();
        private Stack<int> variablesOffsetStack = new Stack<int>();
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

        #endregion
        
        #region Scopes work

        private ParseTreeProperty<Scope> scopes { get; }
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
            AddAllocateStackFrame4000();
            PushFunc(name);
        }

        public void AddFunctionEnd(string name)
        {
            var label = $"func_{name}_end";
            _code += $"\n{label}:";
            _code += $"\n\tdealloc_return;";
            PopFunc();
        }

        public void AddReturn(string funcName)
        {
            var label = $"func_{funcName}_end";
            AddJump(label);
        }

        public void AddReturnValue(string sourceRegister)
        {
            AddRegisterToRegisterAssign("r0", sourceRegister);
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

        public void AddConditionalContinue(string loopName, string pRegister, bool negate = false)
        {
            var label = $"loop_{loopName}_start";
            AddConditionalJump(pRegister, label, negate);
        }

        public void AddBreak(string loopName)
        {
            var label = $"loop_{loopName}_end";
            AddJump(label);
        }

        public void AddConditionalBreak(string loopName, string pRegister, bool negate = false)
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

        public void AddConditionalJumpToElse(string ifName, string pRegister, bool negate = false)
        {
            var label = $"if_{ifName}_else";
            AddConditionalJump(pRegister, label, negate);
        }

        public void AddJumpToIfEnd(string ifName)
        {
            var label = $"if_{ifName}_end";
            AddJump(label);
        }

        public void AddConditionalJumpToIfEnd(string ifName, string pRegister, bool negate = false)
        {
            var label = $"if_{ifName}_end";
            AddConditionalJump(pRegister, label, negate);
        }
        
        #endregion

        #region Read-write variables to register

        public void AddRegisterToVariableWriting(VarSymbol variable, string register)
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

        public void AddVariableAddressToRegisterReading(VarSymbol variable, string register)
        {
            if (variable.IsGlobal)
                _code += $"\n\t{register} = ##{variable.BaseAddress};";
            else
                _code += $"\n\t{register} = add(SP, #{variable.BaseAddress})";
            LastReferencedAddressRegister = register;
            LastReferencedAddressRegisterType = variable.Type;
        }

        public void AddRegisterToVariableWritingWithOffset(VarSymbol variable, string register, string offset)
        {
            var memRegister = GetFreeRegister();
            var memFunc = variable.Type.MemFunc;
            if (memFunc.Length == 0)
                memFunc = SymbolType.GetType("int").MemFunc;
            if (variable.IsGlobal)
                _code += $"\n\t{memRegister} = ##{variable.BaseAddress};";
            else
                _code += $"\n\t{memRegister} = add(SP + #{variable.BaseAddress});";
            _code += $"\n\t{memFunc}({memRegister} + {offset}) = {register};";
            // LastReferencedVariable = variable;
            // LastAssignedOffsetRegister = offset;
            FreeRegister(memRegister);
        }

        public void AddMemToRegisterReading(string addressRegister, SymbolType type, string destRegister, string offsetRegister = "")
        {
            var memFunc = type.MemFunc;
            var offsetSuffix = offsetRegister == "" ? "" : $" + {offsetRegister}";
            _code += $"\n\t{destRegister} = {memFunc}({addressRegister}{offsetSuffix});";
            LastAssignedRegister = destRegister;
        }

        public void AddRegisterToMemWriting(string addressRegister, SymbolType type, string sourceRegister, string offsetRegister = "")
        {
            var memFunc = type.MemFunc;
            var offsetSuffix = offsetRegister == "" ? "" : $" + {offsetRegister}";
            _code += $"\n\t{memFunc}({addressRegister}{offsetSuffix}) = {sourceRegister};";
        }
        
        #endregion
        
        #region Working with registers
        public void AddValueToRegisterAssign(string register, string value, SymbolType type)
        {
            if (type.Name == "float")
                _code += $"\n\t{register} = sfmake(#{value}):pos;";
            else 
                _code += $"\n\t{register} = #{value};";
            LastAssignedRegister = register;
        }

        public void AddRegisterToRegisterAssign(string lhs, string rhs)
        {
            _code += $"\n\t{lhs} = {rhs};";
            LastAssignedRegister = lhs;
        }

        public void AddConditionalRegisterToRegisterAssign(string pRegister, string destRegister, 
            string sourceRegisterIfTrue, string sourceRegisterIfFalse)
        {
            _code += $"\n\tif({pRegister}) {destRegister} = {sourceRegisterIfTrue}";
            _code += $"\n\tif(!{pRegister}) {destRegister} = {sourceRegisterIfFalse}";
            LastAssignedRegister = destRegister;
        }
        
        #endregion
        
        #region ALU function
        public void AddAddingRegisterToRegister(string lhs, string s1, string s2, SymbolType type)
        {
            var addFunc = type.AddFunc;
            _code += $"\n\t{lhs} = {addFunc}({s1}, {s2});";
            LastAssignedRegister = lhs;
        }

        public void AddSubRegisterFromRegister(string lhs, string s1, string s2, SymbolType type)
        {
            var subFunc = type.SubFunc;
            _code += $"\n\t{lhs} = {subFunc}({s1}, {s2});";
            LastAssignedRegister = lhs;
        }

        public void AddNegateRegister(string destRegister, string sourceRegister)
        {
            _code += $"\n\t{destRegister} = neg({sourceRegister});";
            LastAssignedRegister = destRegister;
        }

        public void AddRegisterMpyRegister(string destRegister, string r1, string r2, SymbolType type)
        {
            var mpyFunc = type.MpyFunc;
            _code += $"\n\t{destRegister} = {mpyFunc}({r1}, {r2});";
            LastAssignedRegister = destRegister;
        }

        #endregion

        #region Converting types

        public void AddIntRegisterToFloatConvert(string destRegister, string sourceRegister)
        {
            _code += $"\n\t{destRegister} = convert_w2sf({sourceRegister});";
            LastAssignedRegister = destRegister;
        }

        public void AddFloatRegisterToIntConvert(string destRegister, string sourceRegister)
        {
            _code += $"\n\t{destRegister} = convert_sf2w({sourceRegister});";
            LastAssignedRegister = destRegister;
        }

        #endregion
        
        #region Bit manipulation functions
        public void AddRegisterAndRegister(string destRegister, string r1, string r2)
        {
            _code += $"\n\t{destRegister} = and({r1}, {r2});";
            LastAssignedRegister = destRegister;
        }

        public void AddRegisterOrRegister(string destRegister, string r1, string r2)
        {
            _code += $"\n\t{destRegister} = or({r1}, {r2});";
            LastAssignedRegister = destRegister;
        }

        public void AddRegisterXorRegister(string destRegister, string r1, string r2)
        {
            _code += $"\n\t{destRegister} = xor({r1}, {r2});";
            LastAssignedRegister = destRegister;
        }

        public void AddRegisterRightShiftRegister(string destRegister, string r1, string r2)
        {
            _code += $"\n\t{destRegister} = lsr({r1}, {r2});";
            LastAssignedRegister = destRegister;
        }

        public void AddRegisterLefShiftRegister(string destRegister, string r1, string r2)
        {
            _code += $"\n\t{destRegister} = lsl({r1}, {r2});";
            LastAssignedRegister = destRegister;
        }

        public void AddNotRegister(string destRegister, string sourceRegister)
        {
            _code += $"\n\t{destRegister} = not({sourceRegister});";
            LastAssignedRegister = destRegister;
        }
        
        #endregion
        
        #region Adding compares
        public void AddCompareRegisterEqRegister(string pRegister, string r1, string r2, SymbolType type, bool negate = false)
        {
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

        public void AddCompareRegisterEqNumber(string pRegister, string register, string value, SymbolType type, bool negate = false)
        {
            if (type.Name == "float")
            {
                if (negate)
                    throw new NotImplementedException("Negate for float!");
                var flRegister = GetFreeRegister();
                AddValueToRegisterAssign(flRegister, value, type);
                AddCompareRegisterEqRegister(pRegister, register, flRegister, type, negate);
                FreeRegister(flRegister);
            }
            else
            {
                var negateSym = negate ? "!" : "";
                _code += $"\n\t{pRegister} = {negateSym}cmp.eq({register}, #{value});";   
            }
        }

        public void AddCompareRegisterGeRegister(string pRegister, string r1, string r2, SymbolType type)
        {
            var sfPrefix = type.Name == "float" ? "sf" : "";
            _code += $"\n\t{pRegister} = {sfPrefix}cmp.ge({r1}, {r2});";
        }
        
        public void AddCompareRegisterGtRegister(string pRegister, string r1, string r2, SymbolType type)
        {
            var sfPrefix = type.Name == "float" ? "sf" : "";
            _code += $"\n\t{pRegister} = {sfPrefix}cmp.gt({r1}, {r2});";
        }
        
        public void AddCompareRegisterLeRegister(string pRegister, string r1, string r2, SymbolType type)
        {
            // LE делается через GE простым свапом параметров
            var sfPrefix = type.Name == "float" ? "sf" : "";
            _code += $"\n\t{pRegister} = {sfPrefix}cmp.ge({r2}, {r1});";
        }
        
        public void AddCompareRegisterLtRegister(string pRegister, string r1, string r2, SymbolType type)
        {
            // LT делается через GT простым свапом параметров
            var sfPrefix = type.Name == "float" ? "sf" : "";
            _code += $"\n\t{pRegister} = {sfPrefix}cmp.gt({r2}, {r1});";
        }
        
        #endregion
        
        #region Adding calls, jumps and returns
        public void AddJump(string label)
        {
            _code += $"\n\tjump {label};";
        }

        public void AddConditionalJump(string pRegister, string label, bool negate = false)
        {
            var negationSym = negate ? "!" : "";
            _code += $"\nif({negationSym}{pRegister}) jump {label}";
        }

        public void AddJumpToRegister(string register)
        {
            _code += $"\n\tjumpr {register};";
        }

        public void AddCall(string label)
        {
            _code += $"\n\tcall {label};";
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