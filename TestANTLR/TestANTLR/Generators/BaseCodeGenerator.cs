using System;
using Antlr4.Runtime;

namespace TestANTLR.Generators
{
    public abstract class BaseCodeGenerator: ICodeGenerator
    {
        public abstract AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode);
    }
}