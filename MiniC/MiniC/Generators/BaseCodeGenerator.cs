using System;
using Antlr4.Runtime;

namespace MiniC.Generators
{
    public abstract class BaseCodeGenerator: ICodeGenerator
    {
        public abstract AsmCodeWriter GenerateCodeForContext(ParserRuleContext context, AsmCodeWriter currentCode);

        protected Register getValueFromExpression(AsmCodeWriter currentCode, bool freeAddressRegister = true)
        {
            if (currentCode.LastReferencedAddressRegister == null)
                return currentCode.LastAssignedRegister;
            
            // Получаем регистр с адресом значения
            var valueAddressRegister = currentCode.LastReferencedAddressRegister;
            var valueType = valueAddressRegister.Type;

            // Записываем значение переменной в регистр
            var valueRegister = currentCode.GetFreeRegister();
            currentCode.AddMemToRegisterReading(valueAddressRegister, valueType, valueRegister);

            // Чистим регистр адреса
            if (freeAddressRegister)
                currentCode.FreeLastReferencedAddressRegister();

            return valueRegister;
        }

        protected void convertTypeIfNeeded(AsmCodeWriter currentCode, Register valueRegister, ParserRuleContext context)
        {
            var valueTypeToConvert = currentCode.Conversions.Get(context);
            if (valueTypeToConvert != null)
                currentCode.ConvertRegisterToType(valueRegister, valueRegister, valueTypeToConvert);
        }
    }
}