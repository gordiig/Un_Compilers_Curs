using Antlr4.Runtime;
using MiniC.Exceptions;
using MiniC.Scopes;

namespace MiniC.Generators.Expressions
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

                if (symbol.Type.IsStructType())
                    currentCode.LastReferencedStructType = symbol.Type;
                currentCode.LastReferencedSymbol = symbol;

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
                var inBracesValueRegister = getValueFromExpression(currentCode);
                
                // Привод типа к int в скобках если нужно
                convertTypeIfNeeded(currentCode, inBracesValueRegister, ternaryExpr);

                // Вычисление lValue
                var lvalExprGen = new LValueExpressionGenerator();
                currentCode = lvalExprGen.GenerateCodeForContext(lvalExpr, currentCode);
                var lValueAddressRegister = currentCode.LastReferencedAddressRegister;
                var lValueType = lValueAddressRegister.Type;
                
                // Получаем адрес нулевого элемента (то есть читаем значение текущего регистра), если массив не глобальный
                if (currentCode.GlobalScope.GetSymbol(currentCode.LastReferencedSymbol.Name) == null)
                    currentCode.AddMemToRegisterReading(lValueAddressRegister, SymbolType.GetType("int"), 
                        lValueAddressRegister);
                
                // Вычисление оффсета для массива
                var intType = SymbolType.GetType("int");
                var varSizeRegister = currentCode.GetFreeRegister();
                currentCode.AddValueToRegisterAssign(varSizeRegister, lValueType.Size.ToString(), intType);
                var offsetRegister = currentCode.GetFreeRegister();
                currentCode.AddRegisterMpyRegister(offsetRegister, inBracesValueRegister, varSizeRegister);

                // Вычисление адреса индексированного элемента
                currentCode.AddAddingRegisterToRegister(lValueAddressRegister, lValueAddressRegister, offsetRegister);
                
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
                var lValueType = lValueAddressRegister.Type;
                var structType = currentCode.LastReferencedStructType;

                currentCode.AddComment($"Getting dot value (.{identifier.GetText()})");
                
                // Вычисление offset для переменной структуры
                var structSymbol = currentCode.GlobalScope.FindStruct(structType);
                var structOffset = structSymbol.VariableOffsetFromStartAddress(identifier.GetText());

                var intType = SymbolType.GetType("int");
                var offsetRegister = currentCode.GetFreeRegister();
                currentCode.AddValueToRegisterAssign(offsetRegister, structOffset.ToString(), intType);
                
                // Получаем адрес нужной переменной в структуре
                currentCode.AddAddingRegisterToRegister(lValueAddressRegister, lValueAddressRegister, offsetRegister);
                currentCode.LastReferencedAddressRegister.Type = structSymbol.VariableType(identifier.GetText());

                // Чиска регистров
                currentCode.FreeRegister(offsetRegister);
            }
            
            return currentCode;
        }
    }
}