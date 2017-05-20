using System.Linq.Expressions;
using System.Reflection;

namespace Daemos.Query
{
    public partial class TransactQueryParser
    {
        public ParameterExpression Transaction { get; } = Expression.Parameter(typeof(Transaction));
        public static MemberExpression GetPropertyCI(Expression owner, string name)
        {
            return Expression.Property(owner, owner.Type.GetProperty(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public));
        }

        private static Expression In(Expression lhs, Expression rhs)
        {
            var method = rhs.Type.GetMethod("Contains", new[] { lhs.Type });

            return Expression.Call(rhs, method, lhs);
        }

        private string Unescape(string input, char tokenBarrier)
        {
            var builder = new System.Text.StringBuilder(input.Length);

            bool lastWasSlash = false;
            int lastCodePoint = 0;

            for (int i = 0; i < input.Length; ++i)
            {
                char c = input[i];
                if (c == '\\')
                {
                    lastWasSlash = !lastWasSlash;
                }
                else if (lastWasSlash)
                {
                    int copyLen = i - lastCodePoint - 1;
                    builder.Append(input.Substring(lastCodePoint, copyLen));

                    if (c == 'n')
                    {
                        builder.Append('\n');
                    }
                    else if (c == 'r')
                    {
                        builder.Append('\r');
                    }
                    else if (c == tokenBarrier)
                    {
                        builder.Append(tokenBarrier);
                    }
                    else if (c == 't')
                    {
                        builder.Append('\t');
                    }
                    lastCodePoint = i + 1;
                    lastWasSlash = false;
                }

            }

            if (lastCodePoint < input.Length)
            {
                builder.Append(input.Substring(lastCodePoint));
            }

            return builder.ToString();
        }
    }
}
