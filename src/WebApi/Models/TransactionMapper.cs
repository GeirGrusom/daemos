﻿// <copyright file="TransactionMapper.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos.WebApi.Models
{
    internal static class TransactionMapper
    {
        internal static TransactionResult ToTransactionResult(this Transaction input)
        {
            return new TransactionResult(input);
        }
    }
}
