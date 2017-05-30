using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Daemos.Scripting;

namespace Daemos.Console
{
    public class InitializeResult : ISerializable
    {
        public InitializeResult(int transactionId)
        {
            TransactionId = transactionId;
        }

        public InitializeResult(IStateDeserializer deserializer)
        {
            TransactionId = deserializer.Deserialize<int>(nameof(TransactionId));
        }

        public int TransactionId { get; }
        public void Serialize(IStateSerializer serializer)
        {
            serializer.Serialize(nameof(TransactionId), TransactionId);
        }
    }

    public class AuthorizeResult : ISerializable
    {
        public AuthorizeResult(decimal amount)
        {
            Amount = amount;
        }

        public AuthorizeResult(IStateDeserializer deserializer)
        {
            Amount = deserializer.Deserialize<decimal>(nameof(Amount));
        }

        public decimal Amount { get; }
        public void Serialize(IStateSerializer serializer)
        {
            serializer.Serialize(nameof(Amount), Amount);
        }
    }

    public class CaptureResult : ISerializable
    {
        public CaptureResult(decimal capturedAmount, decimal remainingAmount)
        {
            CapturedAmount = capturedAmount;
            RemainingAmount = remainingAmount;
        }

        public CaptureResult(IStateDeserializer deserializer)
        {
            CapturedAmount = deserializer.Deserialize<decimal>(nameof(CapturedAmount));
            RemainingAmount = deserializer.Deserialize<decimal>(nameof(RemainingAmount));
        }

        public decimal CapturedAmount { get; }
        public decimal RemainingAmount { get; }
        public void Serialize(IStateSerializer serializer)
        {
            serializer.Serialize(nameof(CapturedAmount), CapturedAmount);
            serializer.Serialize(nameof(RemainingAmount), RemainingAmount);
        }
    }

    public class CompleteResult
    {
        public CompleteResult(int transactionId)
        {
            TransactionId = transactionId;
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
                MerchantId = merchantId;
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
