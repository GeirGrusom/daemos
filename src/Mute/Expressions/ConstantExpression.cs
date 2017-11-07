// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using System;
    using System.Globalization;
    using Antlr4.Runtime;

    public class ConstantExpression : Expression
    {
        public object Value { get; }

        public ConstantExpression(object value, ParserRuleContext context) : base(DataType.FromClrType(value?.GetType() ?? typeof(object)), context)
        {
            this.Value = value;
        }

        public ConstantExpression(DataType type, object value, ParserRuleContext context) : base(type, context)
        {
            this.Value = value;
        }

        public static readonly ConstantExpression TrueExpression = new ConstantExpression(DataType.NonNullBool, true, ParserRuleContext.EmptyContext);
        public static readonly ConstantExpression FalseExpression = new ConstantExpression(DataType.NonNullBool, false, ParserRuleContext.EmptyContext);
        public static readonly ConstantExpression NullExpression = new ConstantExpression(null, ParserRuleContext.EmptyContext);
        public static readonly ConstantExpression EmptyString = new ConstantExpression("", ParserRuleContext.EmptyContext);

        public override string ToString()
        {
            if (this.Value is IFormattable formattable)
            {
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            }
            return this.Value.ToString();
        }
    }
}
