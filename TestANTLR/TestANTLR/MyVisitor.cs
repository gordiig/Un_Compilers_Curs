using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestANTLR
{
    public class MyVisitor : MiniCBaseVisitor<object>
    {
        public override object VisitCompilationUnit(MiniCParser.CompilationUnitContext context)
        {
            Console.WriteLine("MyVisitor VisitCompileUnit");
            context
                .children
                .OfType<TerminalNodeImpl>()
                .ToList()
                .ForEach(child => Visit(child));
            return null;
        }

        private void Visit(TerminalNodeImpl node)
        {
            Console.WriteLine(" Visit Symbol={0}", node.Symbol.Text);
        }
    }
}
