using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using Antlr4.Runtime;

namespace Markurion.Mute.Expressions
{
    public class ObjectExpression : Expression
    {
        public List<ObjectMember> Members { get; }

        public ObjectExpression(DataType resultType, IEnumerable<ObjectMember> members, ParserRuleContext context) : base(resultType, context)
        {
            Members = members.ToList();
        }

        public ObjectExpression(IEnumerable<ObjectMember> members, ParserRuleContext context) : base(DataType.FromClrType(typeof(ExpandoObject)), context)
        {
            Members = members.ToList();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("{ ");
            foreach (var item in Members)
            {
                sb.Append(item.Name);
                sb.Append(": ");
                sb.Append(item.Value);
            }
            sb.Append(" }");
            return sb.ToString();
        }
    }

    public class ObjectMember : Expression
    {
        public string Name { get; }
        public Expression Value { get; }

        public ObjectMember(string name, Expression value, ParserRuleContext context)
            : base(value.Type, context)
        {
            Name = name;
            Value = value;
        }
    }
}
