using System;
using System.Collections.Generic;
using System.Text;

namespace MiniC.Scopes
{
    public class LocalScope : Scope
    {
        public LocalScope(Scope parent) : base(parent)
        {

        }
    }
}
