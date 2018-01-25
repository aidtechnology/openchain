using System;

namespace Openchain
{
    public class TransactionFilter
    {
        public TransactionFilter()
        {
            this.StartDate = DateTime.MinValue;
            this.EndDate = DateTime.MaxValue;
        }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
