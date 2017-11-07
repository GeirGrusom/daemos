// <copyright file="MatchCompiler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Linq.Expressions;

namespace Daemos.Query
{
    public class MatchCompiler : ITransactionMatchCompiler
    {
        [return: CanBeNull]
        public Expression<Func<Transaction, bool>> BuildExpression([NotNull] string input)
        {
            var transactQueryLexer = new TransactQueryLexer(new Antlr4.Runtime.AntlrInputStream(input));
            var parser = new TransactQueryParser(new Antlr4.Runtime.BufferedTokenStream(transactQueryLexer));

            var result = parser.compileUnit();

            if (result == null || result.expr == null)
            {
                return null;
            }

            return Expression.Lambda<Func<Transaction, bool>>(result.expr, parser.Transaction);
        }
    }
}
