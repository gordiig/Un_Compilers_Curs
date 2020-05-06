using System;
using System.Collections.Generic;
using System.Text;

namespace TestANTLR.Scopes
{
    public class VarSymbol : ISymbol
    {
        public string Name { get; }
        public SymbolType Type { get; } 
        public bool IsConst { get; }
        public int ArraySize { get; }

        public VarSymbol(string name, SymbolType type, bool isConst = false, int arraySize = -1)
        {
            Name = name;
            Type = type;
            IsConst = isConst;
            ArraySize = arraySize;
        }
    }
}
