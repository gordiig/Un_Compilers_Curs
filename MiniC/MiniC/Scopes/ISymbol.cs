using System;
using System.Collections.Generic;
using System.Text;

namespace MiniC.Scopes
{
    public interface ISymbol
    {
        string Name { get; }
        SymbolType Type { get; }
        int ArraySize { get; }

        string BaseAddress { get; set; }

        bool IsGlobal { get; set; }

        int Size { get; }
    }
}
