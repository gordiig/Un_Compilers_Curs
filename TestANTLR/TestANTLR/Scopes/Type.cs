using System;
using System.Collections.Generic;
using System.Text;
using TestANTLR.Exceptions;

namespace TestANTLR.Scopes
{
    public class SymbolType
    {
        private static List<SymbolType> types = new List<SymbolType>();
        private static HashSet<string> setTypes = new HashSet<string>();

        private string name;

        private SymbolType(string type)
        {
            name = type;
        }

        public string TypeName()
        {
            return name;
        }

        public static SymbolType GetType(string type)
        {
            return types.Find(t => t.name == type);
        }

        public static void AddType(string type)
        {
            if (!setTypes.Contains(type))
            {
                types.Add(new SymbolType(type));
                setTypes.Add(type);
            }
        }

        public static void AddTypeRange(params string[] types)
        {
            foreach (var type in types)
            {
                if (!setTypes.Contains(type))
                {
                    SymbolType.types.Add(new SymbolType(type));
                    setTypes.Add(type);
                }
            }
        }

        public static bool CheckType(string type)
        {
            return setTypes.Contains(type);
        }
    }
}
