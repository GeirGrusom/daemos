// <copyright file="IPaymentService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos.Console
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Daemos.Scripting;

    public class InitializeResult : ISerializable
    {
        public InitializeResult(int transactionId)
        {
            this.TransactionId = transactionId;
        }

        public InitializeResult(IStateDeserializer deserializer)
        {
            this.TransactionId = deserializer.Deserialize<int>(nameof(this.TransactionId));
        }

        public int TransactionId { get; }

        public void Serialize(IStateSerializer serializer)
        {
            serializer.Serialize(nameof(this.TransactionId), this.TransactionId);
        }
    }

    public class AuthorizeResult : ISerializable
    {
        public AuthorizeResult(decimal amount)
        {
            this.Amount = amount;
        }

        public AuthorizeResult(IStateDeserializer deserializer)
        {
            this.Amount = deserializer.Deserialize<decimal>(nameof(this.Amount));
        }

        public decimal Amount { get; }

        public void Serialize(IStateSerializer serializer)
        {
            serializer.Serialize(nameof(this.Amount), this.Amount);
        }
    }

    public class CaptureResult : ISerializable
    {
        public CaptureResult(decimal capturedAmount, decimal remainingAmount)
        {
            this.CapturedAmount = capturedAmount;
            this.RemainingAmount = remainingAmount;
        }

        public CaptureResult(IStateDeserializer deserializer)
        {
            this.CapturedAmount = deserializer.Deserialize<decimal>(nameof(this.CapturedAmount));
            this.RemainingAmount = deserializer.Deserialize<decimal>(nameof(this.RemainingAmount));
        }

        public decimal CapturedAmount { get; }

        public decimal RemainingAmount { get; }

        public void Serialize(IStateSerializer serializer)
        {
            serializer.Serialize(nameof(this.CapturedAmount), this.CapturedAmount);
            serializer.Serialize(nameof(this.RemainingAmount), this.RemainingAmount);
        }
    }

    public class CompleteResult
    {
        public CompleteResult(int transactionId)
        {
            this.TransactionId = transactionId;
        }

        public int TransactionId { get; }
    }

    public interface IPaymentService
    {
        InitializeResult Initialize(int merchantId);

        AuthorizeResult Authorize(int transactionId, decimal maxAmount);

        CaptureResult Capture(int transactionId, decimal amount);

        CompleteResult Complete(int transactionId);
    }

    public class MockPaymentService : IPaymentService
    {
        private class TransactionInfo
        {
            public decimal AuthorizedAmount { get; set; }

            public int MerchantId { get; }

            public TransactionInfo(int merchantId)
            {
                this.MerchantId = merchantId;
            }

        }

        private static readonly Random RandomGenerator = new Random();
        private static readonly Dictionary<int, TransactionInfo> _transactions = new Dictionary<int, TransactionInfo>();

        public InitializeResult Initialize(int merchantId)
        {
            var transactionId = RandomGenerator.Next();
            _transactions.Add(transactionId, new TransactionInfo(merchantId));
            return new InitializeResult(transactionId);
        }

        public AuthorizeResult Authorize(int transactionId, decimal maxAmount)
        {
            var ti = _transactions[transactionId];
            ti.AuthorizedAmount = maxAmount;
            return new AuthorizeResult(maxAmount);
        }

        public CaptureResult Capture(int transactionId, decimal amount)
        {
            var ti = _transactions[transactionId];
            var resultAmount = ti.AuthorizedAmount - amount;
            if (resultAmount < 0m)
            {
                throw new InvalidOperationException("Cannot capture more than the authorized amount.");
            }
            ti.AuthorizedAmount = resultAmount;
            return new CaptureResult(amount, ti.AuthorizedAmount);
        }

        public CompleteResult Complete(int transactionId)
        {
            var ti = _transactions[transactionId];
            ti.AuthorizedAmount = 0m;
            return new CompleteResult(transactionId);
        }
    }
}
