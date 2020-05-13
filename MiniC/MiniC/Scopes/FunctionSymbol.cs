using System;
using System.Collections.Generic;
using System.Text;

namespace MiniC.Scopes
{
    public class FunctionSymbol : Scope, ISymbol
    {
        public string Name { get; }
        public SymbolType Type { get; }
        public int ArraySize { get; } = -1;

        public string BaseAddress { get; set; }
        public bool IsGlobal { get; set; }

        public FunctionSymbol(string name, SymbolType type, Scope parent) : base(parent)
        {
            Name = name;
            Type = type;
            BaseAddress = "0";
            IsGlobal = true;
        }
        
        public int Size { get => 0; }
    }
}
