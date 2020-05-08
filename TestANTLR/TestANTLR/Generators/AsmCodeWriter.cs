using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TestANTLR.Generators
{
    public class AsmCodeWriter
    {
        private string _variables = "";
        private string _code = "";
        
        public string Code { get => _code; }
        public string Variables { get => _variables; }
        public string AllCode { get => "\t.section\t.data" + Variables + "\n\n\t.section\t.text" + Code + "\n"; }

        public AsmCodeWriter()
        {
            AvaliableRegisters = new bool[27];
            for (int i = 0; i < AvaliableRegisters.Length; i++)
                AvaliableRegisters[i] = true;
        }
        
        // Registers work
        public string LastAssignedRegister = "r2";
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
                    var ans = $"r{i + 2}";
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
            AvaliableRegisters[idx - 2] = true;
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

        // Useful privates
        private string memFuncForType(string type)
        {
            switch (type)
            {
                case "int":
                    return "memw";
                case "float":
                    return "memw";
                case "char":
                    return "memb";
                default:
                    throw new ArgumentException("Unknown type");
            }
        }
        
        private string mpyFuncForType(string type)
        {
            switch (type)
            {
                case "int":
                    return "mpyi";
                case "float":
                    // TODO: MPY for float
                    throw new NotImplementedException("Mpy for float");
                case "char":
                    return "mpyi";
                default:
                    throw new ArgumentException("Unknown type");
            }
        }

        // Writing to file
        public void WriteToFile(string filename)
        {
            using (var writer = new StreamWriter(File.Open(filename, FileMode.Create)))
            {
                writer.AutoFlush = true;
                writer.Write(AllCode);
            }
        }

        // Adding variables
        public void AddVariable(string name, string type, string value = "0")
        {
            _variables += $"\n{name}:\n\t.{type}\t{value}";
        }

        public void AddEmptyArray(string name, string type, int capacity)
        {
            var header = $"\n{name}:\n\t.{type} ";
            for (int i = 0; i < capacity; i++)
            {
                header += "0, ";
            }
            if (capacity > 0)
                header = header.Remove(header.Length - 2, 2);
            _variables += header;
        }

        public void AddArray(string name, string type, string values)
        {
            _variables += $"\n{name}:\n\t.{type} {values}";
        }

        // Adding function labels
        public void AddFunctionStart(string name)
        {
            var label = $"func_{name}_start";
            _code += $"\n{label}:";
            FuncStack.Push(name);
        }

        public void AddFunctionEnd(string name)
        {
            var label = $"func_{name}_end";
            _code += $"\n{label}:";
            _code += $"\n\tjump LR;";
            FuncStack.Pop();
        }

        public void AddReturn(string funcName)
        {
            var label = $"func_{funcName}_end";
            AddJump(label);
        }

        public void AddReturnValue(string sourceRegister)
        {
            AddRegisterToRegisterAssign("r1", sourceRegister);
        }

        // Adding loop labels
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

        // Adding if labels
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

        // Read-write variables to register
        public void AddVariableToRegisterReading(string variableName, string type, string register)
        {
            var memFunc = memFuncForType(type);
            _code += $"\n\tr0 = ##{variableName};";
            _code += $"\n\t{register} = {memFunc}(r0);";
            LastAssignedRegister = register;
            LastReferencedVariable = variableName;
        }

        public void AddRegisterToVariableWriting(string variableName, string type, string register)
        {
            var memFunc = memFuncForType(type);
            _code += $"\n\tr0 = ##{variableName};";
            _code += $"\n\t{memFunc}(r0) = {register};";
            LastReferencedVariable = variableName;
        }

        public void AddArrayToRegisterReading(string variableName, string type, string register, string offset)
        {
            // Offset ВСЕГДА РЕГИСТР ИЗ-ЗА УМНОЖЕНИЯ
            var memFunc = memFuncForType(type);
            _code += $"\n\tr0 = ##{variableName};";
            _code += $"\n\t{register} = {memFunc}(r0 + {offset});";
            LastAssignedRegister = register;
            LastReferencedVariable = variableName;
        }

        public void AddRegisterToArrayWriting(string variableName, string type, string register, string offset)
        {
            // ТО ЖЕ САМОЕ ДЛЯ Offset
            var memFunc = memFuncForType(type);
            _code += $"\n\tr0 = ##{variableName};";
            _code += $"\n\t{memFunc}(r0 + {offset}) = {register};";
            LastReferencedVariable = variableName;
        }

        public void AddMemToRegisterReading(string addressRegister, string type, string destRegister, string offsetRegister = "")
        {
            var memFunc = memFuncForType(type);
            var offsetSuffix = offsetRegister == "" ? "" : $" + {offsetRegister}";
            _code += $"\n\t{destRegister} = {memFunc}({addressRegister}{offsetSuffix});";
            LastAssignedRegister = destRegister;
        }

        public void AddRegisterToMemWriting(string addressRegister, string type, string sourceRegister, string offsetRegister = "")
        {
            var memFunc = memFuncForType(type);
            var offsetSuffix = offsetRegister == "" ? "" : $" + {offsetRegister}";
            _code += $"\n\tr0 = {sourceRegister}";
            _code += $"\n\t{memFunc}({addressRegister}{offsetSuffix}) = {sourceRegister};";
        }
        
        // Working with registers
        public void AddValueToRegisterAssign(string register, string value)
        {
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
        
        // ALU function
        public void AddAddingRegisterToRegister(string lhs, string s1, string s2)
        {
            _code += $"\n\t{lhs} = add({s1}, {s2});";
            LastAssignedRegister = lhs;
        }

        public void AddSubRegisterFromRegister(string lhs, string s1, string s2)
        {
            _code += $"\n\t{lhs} = sub({s1}, {s2});";
            LastAssignedRegister = lhs;
        }

        public void AddNegateRegister(string destRegister, string sourceRegister)
        {
            _code += $"\n\t{destRegister} = neg({sourceRegister});";
            LastAssignedRegister = destRegister;
        }

        public void AddRegisterMpyRegister(string destRegister, string r1, string r2, string type)
        {
            var mpyFunc = mpyFuncForType(type);
            _code += $"\n\t{destRegister} = {mpyFunc}({r1}, {r2});";
            LastAssignedRegister = destRegister;
        }
        
        public void AddRegisterMpyValue(string destRegister, string register, string value, string type)
        {
            var mpyFunc = mpyFuncForType(type);
            _code += $"\n\t{destRegister} = {mpyFunc}({register}, #{value});";
            LastAssignedRegister = destRegister;
        }
        
        // Bit manipulation functions
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
        
        // Adding compares
        public void AddCompareRegisterEqRegister(string pRegister, string r1, string r2, bool negate = false)
        {
            var negateSym = negate ? "!" : "";
            _code += $"\n\t{pRegister} = {negateSym}cmp.eq({r1}, {r2});";
        }

        public void AddCompareRegisterEqNumber(string pRegister, string register, string value, bool negate = false)
        {
            var negateSym = negate ? "!" : "";
            _code += $"\n\t{pRegister} = {negateSym}cmp.eq({register}, #{value});";
        }
        
        // Adding calls, jumps and returns
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

        // Adding comments
        public void AddInlineComment(string comment)
        {
            _code += $"\t// {comment}";
        }

        public void AddComment(string comment, bool withTab = true)
        {
            var tabSym = withTab ? "\t" : "";
            _code += $"\n{tabSym}// {comment}";
        }

        // Adding plain code
        public void AddPlainCode(string code)
        {
            _code += $"\n{code}";
        }

    }
}