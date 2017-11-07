// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using System;
    using System.Reflection;
    using Antlr4.Runtime;

    public class MemberExpression : Expression
    {
        public Expression Instance { get; }

        public MemberInfo Member { get; }

        private static DataType GetResultTypeFromMemberInfo(MemberInfo info)
        {
            if (info is PropertyInfo)
            {
                return DataType.FromClrType(((PropertyInfo) info).PropertyType);
            }
            if (info is MethodInfo)
            {
                return new DataType(typeof(MethodInfo), false);
            }
            throw new NotSupportedException();
        }

        public MemberExpression(Expression instance, MemberInfo member, ParserRuleContext context)
            : base(GetResultTypeFromMemberInfo(member), context)
        {
            this.Instance = instance;
            this.Member = member;
        }

        public override string ToString()
        {
            return $"{this.Instance}.{this.Member.Name}";
        }
    }
}
