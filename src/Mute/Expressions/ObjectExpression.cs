// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Text;
    using Antlr4.Runtime;

    public class ObjectExpression : Expression
    {
        public List<ObjectMember> Members { get; }

        public ObjectExpression(DataType resultType, IEnumerable<ObjectMember> members, ParserRuleContext context) : base(resultType, context)
        {
            this.Members = members.ToList();
        }

        public ObjectExpression(IEnumerable<ObjectMember> members, ParserRuleContext context) : base(DataType.FromClrType(typeof(ExpandoObject)), context)
        {
            this.Members = members.ToList();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("{ ");
            foreach (var item in this.Members)
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
            this.Name = name;
            this.Value = value;
        }
    }
}
