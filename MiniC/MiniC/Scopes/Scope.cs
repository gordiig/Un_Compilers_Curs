using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniC.Scopes
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

        public bool IsGlobal()
        {
            return Parent == null;
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

        public ISymbol GetSymbol(string name)
        {
            if (Table.TryGetValue(name, out ISymbol symbol))
                return symbol;

            return null;
        }

        /// <summary>
        /// Try to find symbol in current and parents scopes
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ISymbol FindSymbol(string name)
        {
            if (Table.TryGetValue(name, out ISymbol symbol))
                return symbol;
            if (Parent != null)
                return Parent.FindSymbol(name);

            return null;
        }

        public ISymbol GetNumberedSymbol(int i)
        {
            return Table.ElementAt(i).Value;
        }
    }
}
