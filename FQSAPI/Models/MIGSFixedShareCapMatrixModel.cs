namespace FQSAPI.Models
{
    public class MIGSFixedShareCapMatrixModel
    {
        // The identifier for the matrix row
        public int ID { get; set; }

        // The minimum ratio/percentage amount for this range.
        public decimal? LowerLimit { get; set; }

        // The maximum ratio/percentage amount for this range.
        public decimal? UpperLimit { get; set; }

        // Points assigned for meeting this range.
        public int Points { get; set; }
    }
}
