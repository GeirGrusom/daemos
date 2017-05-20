using System;
using System.Reflection;
using Antlr4.Runtime;

namespace Daemos.Mute.Expressions
{
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
            Instance = instance;
            Member = member;
        }

        public override string ToString()
        {
            return $"{Instance}.{Member.Name}";
        }
    }
}
