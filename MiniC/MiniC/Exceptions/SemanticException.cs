using System;
using System.Collections.Generic;
using System.Text;

namespace MiniC.Exceptions
{
    public class SemanticException : Exception
    {
        public SemanticException() :base()
        { }

        public SemanticException(string msg) : base(msg)
        { }

        public SemanticException(string msg, Exception e) : base(msg, e)
        { }
    }
}
