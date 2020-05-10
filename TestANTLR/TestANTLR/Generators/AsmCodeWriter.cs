using System;
using System.Collections.Generic;
using System.IO;
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
        public string AllCode { get => "\t.section\t.data" + Variables + "\n\n\t.section\t.text" + Code + "\n"; }

        public AsmCodeWriter(ParseTreeProperty<Scope> skopes)
        {
            scopes = skopes;
            AvaliableRegisters = new bool[27];
            for (int i = 0; i < AvaliableRegisters.Length; i++)
                AvaliableRegisters[i] = true;
        }
        
        #region Registers work
        
        public string LastAssignedRegister = "r0";
        public string LastReferencedVariable = "";
        public Stack<string> LoopStack = new Stack<string>();
        public Stack<string> IfStack = new Stack<string>();
        public Stack<string> FuncStack = new Stack<string>();
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
        public void WriteToFile(string filename)
        {
            using (var writer = new StreamWriter(File.Open(filename, FileMode.Create)))
            {
                writer.AutoFlush = true;
                writer.Write(AllCode);
            }
        }

        #region Adding variables
        
        public void AddVariable(string name, SymbolType type, string value = "0")
        {
            _variables += $"\n{name}:\n\t.{type.Name}\t{value}";
        }

        public void AddEmptyArray(string name, SymbolType type, int capacity)
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

        public void AddArray(string name, SymbolType type, string values)
        {
            _variables += $"\n{name}:\n\t.{type.Name} {values}";
        }
        
        #endregion
        
        #region Adding function labels
        public void AddFunctionStart(string name)
        {
            var label = $"func_{name}_start";
            _code += $"\n{label}:";
            AddAllocateStackFrame4000();
            FuncStack.Push(name);
        }

        public void AddFunctionEnd(string name)
        {
            var label = $"func_{name}_end";
            _code += $"\n{label}:";
            _code += $"\n\tdealloc_return;";
            FuncStack.Pop();
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
        public void AddVariableToRegisterReading(string variableName, SymbolType type, string register)
        {
            var memRegister = GetFreeRegister();
            var memFunc = type.MemFunc;
            _code += $"\n\t{memRegister} = ##{variableName};";
            _code += $"\n\t{register} = {memFunc}({memRegister});";
            LastAssignedRegister = register;
            LastReferencedVariable = variableName;
            FreeRegister(memRegister);
        }

        public void AddRegisterToVariableWriting(string variableName, SymbolType type, string register)
        {
            var memRegister = GetFreeRegister();
            var memFunc = type.MemFunc;
            _code += $"\n\t{memRegister} = ##{variableName};";
            _code += $"\n\t{memFunc}({memRegister}) = {register};";
            LastReferencedVariable = variableName;
            FreeRegister(memRegister);
        }

        public void AddArrayToRegisterReading(string variableName, SymbolType type, string register, string offset)
        {
            // Offset ВСЕГДА РЕГИСТР ИЗ-ЗА УМНОЖЕНИЯ ???
            var memRegister = GetFreeRegister();
            var memFunc = type.MemFunc;
            _code += $"\n\t{memRegister} = ##{variableName};";
            _code += $"\n\t{register} = {memFunc}({memRegister} + {offset});";
            LastAssignedRegister = register;
            LastReferencedVariable = variableName;
            FreeRegister(memRegister);
        }

        public void AddRegisterToArrayWriting(string variableName, SymbolType type, string register, string offset)
        {
            // ТО ЖЕ САМОЕ ДЛЯ Offset
            var memRegister = GetFreeRegister();
            var memFunc = type.MemFunc;
            _code += $"\n\t{memRegister} = ##{variableName};";
            _code += $"\n\t{memFunc}({memRegister} + {offset}) = {register};";
            LastReferencedVariable = variableName;
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