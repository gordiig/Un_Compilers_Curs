using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using System;
using System.Collections.Generic;
using System.Text;
using MiniC.Exceptions;

namespace MiniC
{
    public class SyntaxErrorListener : BaseErrorListener
    {
        public List<string> ErrorMessages { get; }

        public SyntaxErrorListener()
        {
            ErrorMessages = new List<string>();
        }

        public override void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            //throw new SyntaxException("line " + line + ":" + charPositionInLine + " " + msg);
            ErrorMessages.Add(msg + " at " + line + ":" + charPositionInLine);
        }
    }
}
