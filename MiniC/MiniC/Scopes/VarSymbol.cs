using System;
using System.Collections.Generic;
using System.Text;

namespace MiniC.Scopes
{
    public class VarSymbol : ISymbol
    {
        public string Name { get; }
        public SymbolType Type { get; } 
        public int ArraySize { get; }

        public string BaseAddress { get; set; }
        public bool IsGlobal { get; set; }

        public VarSymbol(string name, SymbolType type, int arraySize = -1)
        {
            Name = name;
            Type = type;
            ArraySize = arraySize;
            BaseAddress = "0";
            IsGlobal = true;
        }

        public int Size { get => ArraySize == -1 ? Type.Size : Type.Size * ArraySize; }
    }
}
