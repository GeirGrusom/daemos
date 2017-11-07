// <copyright file="ITransactionHandlerFactory.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos
{
    public interface ITransactionHandlerFactory
    {
        ITransactionHandler Get(string name);
    }
}