namespace FQSAPI.Models
{
    public class MIGSPast3LoansModel
    {
        // The identifier for the matrix row (Database ID)
        public int LoanMatrixID { get; set; }

        // The status of the past 3 loans (e.g., "All A")
        public string LoanStatus { get; set; }

        // Points assigned, can be negative
        public int Points { get; set; }

        // Weight of the criterion (should be 25.00)
        public decimal Weight { get; set; }
    }
}
