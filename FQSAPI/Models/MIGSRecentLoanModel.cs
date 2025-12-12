namespace FQSAPI.Models
{
    public class MIGSRecentLoanModel
    {
        // The identifier for the matrix row (Database ID)
        public int RecentLoanMatrixID { get; set; }

        // The classification status (e.g., "A", "B-", "New Member or No Loan")
        public string ClassificationStatus { get; set; }

        // Points assigned, can be negative
        public int Points { get; set; }

        // Weight of the criterion (should be 15.00)
        public decimal Weight { get; set; }
    }
}
