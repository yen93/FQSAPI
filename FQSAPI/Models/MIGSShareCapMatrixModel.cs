namespace FQSAPI.Models;


public class MIGSShareCapMatrixModel
{
    // The identifier for the matrix row
    public int ID { get; set; }

    // The minimum share capital amount for this range.
    // decimal? (nullable decimal) handles the NULL value for the lowest range.
    public decimal? LowerLimit { get; set; }

    // The maximum share capital amount for this range.
    // decimal? (nullable decimal) handles the NULL value for the highest range.
    public decimal? UpperLimit { get; set; }

    // Points assigned for meeting this range.
    public int Points { get; set; }
}
