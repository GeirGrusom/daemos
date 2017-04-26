using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;

namespace Markurion.Mute.Expressions
{
    public class ConstantExpression : Expression
    {
        public object Value { get; }
        public ConstantExpression(object value, ParserRuleContext context) : base(DataType.FromClrType(value?.GetType() ?? typeof(object)), context)
        {
            Value = value;
        }

        public ConstantExpression(DataType type, object value, ParserRuleContext context) : base(type, context)
        {
            Value = value;
        }

        public static readonly ConstantExpression TrueExpression = new ConstantExpression(DataType.NonNullBool, true, ParserRuleContext.EmptyContext);
        public static readonly ConstantExpression FalseExpression = new ConstantExpression(DataType.NonNullBool, false, ParserRuleContext.EmptyContext);
        public static readonly ConstantExpression NullExpression = new ConstantExpression(null, ParserRuleContext.EmptyContext);
        public static readonly ConstantExpression EmptyString = new ConstantExpression("", ParserRuleContext.EmptyContext);

        public override string ToString()
        {
            if (Value is IFormattable formattable)
            {
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            }
            return Value.ToString();
        }
    }
}
