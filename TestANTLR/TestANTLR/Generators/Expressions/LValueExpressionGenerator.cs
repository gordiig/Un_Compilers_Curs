using Antlr4.Runtime;
using TestANTLR.Exceptions;
using TestANTLR.Scopes;

namespace TestANTLR.Generators.Expressions
{
    public class LValueExpressionGenerator: BaseCodeGenerator
    {
        public override AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode)
        {
            var lvalExprCtx = context as MiniCParser.LValueExpressionContext;
            var identifier = lvalExprCtx.Identifier();
            var ternaryExpr = lvalExprCtx.ternaryExpression();
            var lvalExpr = lvalExprCtx.lValueExpression();
            // Variable name (a ...)
            if (identifier != null && lvalExprCtx.Dot() == null)
            {
                currentCode.AddComment($"Getting variable \"{identifier.GetText()}\" for lvalue");
                
                // Получаем тип из таблицы
                var curScope = currentCode.GetCurrentScope();
                var symbol = curScope.FindSymbol(identifier.GetText()) as VarSymbol;
                if (symbol == null) 
                    throw new CodeGenerationException($"Unknown symbol {identifier.GetText()}");

                // Запись в регистр
                var register = currentCode.GetFreeRegister();
                currentCode.AddVariableAddressToRegisterReading(symbol, register);
            }
            // Braces (a[] ...)
            else if (ternaryExpr != null)
            {
                currentCode.AddComment("Getting braces value");

                // Вычисления в скобках
                var terExprGen = new TernaryExpressionGenerator();
                currentCode = terExprGen.GenerateCodeForContext(ternaryExpr, currentCode);
                var inBracesValueRegister = currentCode.LastAssignedRegister;
                
                // Вычисление lValue
                var lvalExprGen = new LValueExpressionGenerator();
                currentCode = lvalExprGen.GenerateCodeForContext(lvalExpr, currentCode);
                var lValueAddressRegister = currentCode.LastReferencedAddressRegister;
                var lValueType = currentCode.LastReferencedAddressRegisterType;
                
                // Вычисление оффсета для массива
                var intType = SymbolType.GetType("int");
                var varSizeRegister = currentCode.GetFreeRegister();
                currentCode.AddValueToRegisterAssign(varSizeRegister, lValueType.Size.ToString(), intType);
                var offsetRegister = currentCode.GetFreeRegister();
                currentCode.AddRegisterMpyRegister(offsetRegister, inBracesValueRegister, varSizeRegister, intType);

                // Вычисление адреса индексированного элемента
                currentCode.AddAddingRegisterToRegister(lValueAddressRegister, lValueAddressRegister, offsetRegister, intType);
                
                // Чистка регистров
                currentCode.FreeRegister(offsetRegister);
                currentCode.FreeRegister(varSizeRegister);
                currentCode.FreeRegister(inBracesValueRegister);
            }
            // Dot (a.x ...)
            else
            {
                // Вычисление lvalue
                var lvalExprGen = new LValueExpressionGenerator();
                currentCode = lvalExprGen.GenerateCodeForContext(lvalExpr, currentCode);
                var lValueAddressRegister = currentCode.LastReferencedAddressRegister;
                var lValueType = currentCode.LastReferencedAddressRegisterType;

                currentCode.AddComment($"Getting dot value (.{identifier.GetText()})");
                
                // Вычисление offset для переменной структуры
                var structSymbol = currentCode.GlobalScope.FindStruct(lValueType);
                var structOffset = structSymbol.VariableOffsetFromStartAddress(identifier.GetText());

                var intType = SymbolType.GetType("int");
                var offsetRegister = currentCode.GetFreeRegister();
                currentCode.AddValueToRegisterAssign(offsetRegister, structOffset.ToString(), intType);
                
                // Получаем адрес нужной переменной в структуре
                currentCode.AddAddingRegisterToRegister(lValueAddressRegister, lValueAddressRegister, offsetRegister, intType);
                currentCode.LastReferencedAddressRegisterType = structSymbol.VariableType(identifier.GetText());
                
                // Чиска регистров
                currentCode.FreeRegister(offsetRegister);
            }
            
            return currentCode;
        }
    }
}