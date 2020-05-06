using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestANTLR.Scopes
{
    public abstract class Scope
    {
        private static int scopeInt = 0;

        public Dictionary<string, ISymbol> Table { get; }

        public string ScopeName { get; }
        public Scope Parent { get; }

        public Scope(Scope parent)
        {
            Table = new Dictionary<string, ISymbol>();
            Parent = parent;
            ScopeName = scopeInt.ToString();
            scopeInt++;
        }

        public void AddSymbol(ISymbol sym)
        {
            Table.TryAdd(sym.Name, sym);
        }

        /// <summary>
        /// Checks symbol in current scope
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool CheckSymbol(string name)
        {
            return Table.TryGetValue(name, out ISymbol symbol);
        }

        /// <summary>
        /// Try to find symbol in current and parents scopes
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool FindSymbol(string name)
        {
            if (Table.TryGetValue(name, out ISymbol symbol))
                return true;
            if (Parent != null)
                return Parent.FindSymbol(name);

            return false;
        }
    }
}
