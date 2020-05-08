using System;
using System.Collections.Generic;
using System.Text;

namespace TestANTLR.Scopes
{
    public class FunctionSymbol : Scope, ISymbol
    {
        public string Name { get; }
        public SymbolType Type { get; }
        public int ArraySize { get; } = -1;

        public FunctionSymbol(string name, SymbolType type, Scope parent) : base(parent)
        {
            Name = name;
            Type = type;
        }
    }
}
