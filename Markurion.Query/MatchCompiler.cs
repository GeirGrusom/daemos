using System;
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace Markurion.Query
{
    public class MatchCompiler : ITransactionMatchCompiler
    {
        [return: CanBeNull]
        public Expression<Func<Transaction, bool>> BuildExpression([NotNull] string input)
        {
            var TransactQueryLexer = new TransactQueryLexer(new Antlr4.Runtime.AntlrInputStream(input));
            var parser = new TransactQueryParser(new Antlr4.Runtime.BufferedTokenStream(TransactQueryLexer));

            var result = parser.compileUnit();

            if(result == null || result.expr == null)
            {
                return null;
            }

            return Lambda<Func<Transaction, bool>>(result.expr, parser.Transaction);
        }
    }
}
