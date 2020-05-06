using System;
using System.Collections.Generic;
using System.Text;

namespace TestANTLR.Scopes
{
    public interface ISymbol
    {
        string Name { get; }
        SymbolType Type { get; }
        bool IsConst { get; }
        int ArraySize { get; }
    }
}
