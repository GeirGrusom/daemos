using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;

namespace Markurion.Mute.Expressions
{
    public class BlockExpression : Expression
    {
        public List<VariableExpression> Variables { get; }
        public List<Expression> Body { get; }

        private static DataType DeduceReturnType(IEnumerable<Expression> exp)
        {
            if (exp == null)
            {
                return DataType.Void;
            }
            return DataType.Void;
        }

        public BlockExpression(IEnumerable<Expression> statements, IEnumerable<VariableExpression> variables, ParserRuleContext context)
            : base(DeduceReturnType(statements), context)
        {
            Body = statements?.ToList() ?? new List<Expression>();
            Variables = variables?.ToList() ?? new List<VariableExpression>();
        }

        public override string ToString()
        {
            return $"{{\n{string.Concat(Body.Select(x => x.ToString() + ";\n"))}}}";
        }
    }
}
