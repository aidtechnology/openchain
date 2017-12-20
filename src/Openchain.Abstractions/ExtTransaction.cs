using System;

namespace Openchain
{
    public class ExtTransaction
    {
        public ExtTransaction(ByteString transactionData)
        {
            this.TransactionData = transactionData;
            this.Transaction = MessageSerializer.DeserializeTransaction(transactionData);
            this.Mutation = MessageSerializer.DeserializeMutation(this.Transaction.Mutation);
            this.MutationHash = new ByteString(MessageSerializer.ComputeHash(this.Transaction.Mutation.ToByteArray())).ToString();
            this.TransactionHash = new ByteString(MessageSerializer.ComputeHash(this.TransactionData.ToByteArray())).ToString();
        }

        public string MutationHash { get; set; }
        public string TransactionHash { get; set; }
        public Mutation Mutation { get; set; }
        public Transaction Transaction { get; set; }
        public ByteString TransactionData { get; set; }
    }
}
