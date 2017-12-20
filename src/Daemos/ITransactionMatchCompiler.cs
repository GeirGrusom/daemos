// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos
{
    using System;
    using System.Linq.Expressions;

    /// <summary>
    /// Defines the contract for transaction match compilers
    /// </summary>
    public interface ITransactionMatchCompiler
    {
        /// <summary>
        /// Builds an expression tree from an input string
        /// </summary>
        /// <param name="input">Input string to parse</param>
        /// <returns>Expression tree representing the input query</returns>
        Expression<Func<Transaction, bool>> BuildExpression(string input);
    }
}