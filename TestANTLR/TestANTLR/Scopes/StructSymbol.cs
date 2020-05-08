using System;
using System.Collections.Generic;
using System.Text;

namespace TestANTLR.Scopes
{
    public class StructSymbol : Scope, ISymbol
    {
        public string Name { get; }
        public SymbolType Type { get; }
        public int ArraySize { get; } = -1;

        public StructSymbol(string name, Scope parent) : base(parent)
        {
            Name = name;
            Type = SymbolType.AddType(name);
        }
    }
}
