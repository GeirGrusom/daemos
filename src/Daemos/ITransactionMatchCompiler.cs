// <copyright file="ITransactionMatchCompiler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Linq.Expressions;

namespace Daemos
{
    public interface ITransactionMatchCompiler
    {
        Expression<Func<Transaction, bool>> BuildExpression(string input);
    }
}