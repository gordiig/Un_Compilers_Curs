using System;
using System.Collections.Generic;
using System.Text;

namespace TestANTLR.Scopes
{
    public class GlobalScope : Scope
    {
        public GlobalScope() : base(null)
        {

        }

        public StructSymbol FindStruct(SymbolType structName)
        {
            if (Table.TryGetValue(structName.TypeName(), out ISymbol structSymbol))
                return (StructSymbol) structSymbol;

            return null;
        }
    }
}
