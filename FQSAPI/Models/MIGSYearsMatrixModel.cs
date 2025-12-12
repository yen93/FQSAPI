namespace FQSAPI.Models
{
    public class MIGSYearsMatrixModel
    {
        // The identifier for the matrix row (ID from the table)
        public int ID { get; set; }

        // The minimum years of membership for this range.
        // decimal? (nullable decimal) handles the NULL value for the lowest range.
        public decimal? LowerLimit { get; set; }

        // The maximum years of membership for this range.
        // decimal? (nullable decimal) handles the NULL value for the highest range.
        public decimal? UpperLimit { get; set; }

        // Points assigned for meeting this range.
        public int Points { get; set; }
    }
}
