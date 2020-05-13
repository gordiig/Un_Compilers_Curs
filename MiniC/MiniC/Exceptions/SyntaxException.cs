using System;
using System.Collections.Generic;
using System.Text;

namespace MiniC.Exceptions
{
    public class SyntaxException : Exception
    {
        public SyntaxException() :base()
        { }

        public SyntaxException(string msg) : base(msg)
        { }

        public SyntaxException(string msg, Exception e) : base(msg, e)
        { }
    }
}
