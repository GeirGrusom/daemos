using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
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
