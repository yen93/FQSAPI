namespace FQSAPI.Models
{
    public class MemberMIGSDataModel
    {
        public string Name { get; set; }
        public string HowToImproveRating { get; set; }

        // Points based on Years of Membership
        public int MIGSYearsPoints { get; set; }

        // Points based on Share Capital
        public int MIGSShareCapPoints { get; set; }

        // Points based on Fixed Share Capital (e.g., Share Cap Deposit)
        public int MIGSFixedShareCapPoints { get; set; }

        // Points based on Average Daily Balance (ADB)
        public int MIGSADBPoints { get; set; }

        // Points related to loan penalties paid in the past 3 years
        public int MIGSPast3LoansPoints { get; set; }

        // Points related to recent loan penalties paid
        public int MIGSRecentLoanPoints { get; set; }

        // The final rating or classification (e.g., 'A', 'B', 'C', 'Excellent', 'Poor')
        public string StarRating { get; set; }

        // The sum of all individual point fields
        public int TotalPoints { get; set; }
    }
}
