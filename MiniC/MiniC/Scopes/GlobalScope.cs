using System;
using System.Collections.Generic;
using System.Text;

namespace MiniC.Scopes
{
    public class GlobalScope : Scope
    {
        public GlobalScope() : base(null)
        {

        }

        public StructSymbol FindStruct(SymbolType structName)
        {
            if (Table.TryGetValue(structName.Name, out ISymbol structSymbol))
                return (StructSymbol) structSymbol;

            return null;
        }
    }
}
