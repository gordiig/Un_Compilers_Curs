using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniC.Exceptions;

namespace MiniC.Scopes
{
    public class StructSymbol : Scope, ISymbol
    {
        public string Name { get; }
        public SymbolType Type { get; }
        public int ArraySize { get; } = -1;

        public string BaseAddress { get; set; }
        public bool IsGlobal { get; set; }

        public StructSymbol(string name, Scope parent) : base(parent)
        {
            Name = name;
            Type = SymbolType.AddType(name);
            BaseAddress = "0";
            IsGlobal = true;
        }

        public int Size { get => Table.Sum(d => d.Value.Size); }

        public int VariableOffsetFromStartAddress(string variable)
        {
            int ans = 0;
            foreach (var symKeyValue in Table)
            {
                var symName = symKeyValue.Key;
                if (symName == variable)
                    return ans;
                var sym = symKeyValue.Value;
                ans += sym.Size;
            }
            throw new CodeGenerationException($"Can't calculate struct offset for variable {variable} in {Name}");
        }

        public SymbolType VariableType(string variable)
        {
            return Table[variable].Type;
        }
    }
}
