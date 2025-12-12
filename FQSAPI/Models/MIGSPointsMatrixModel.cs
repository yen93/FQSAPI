using System;

public class MIGSPointsMatrixModel
{
    // Mapped from: MatrixID (int) - Assumed to be the Primary Key
    public int MatrixID { get; set; }

    // Mapped from: MIGSClassification (nvarchar(50))
    // This will likely hold values like 'A', 'B', 'C', etc.
    public string MIGSClassification { get; set; }

    // Mapped from: MinimumPoints (nvarchar(50))
    // NOTE: Although this is a range of points, it's stored as an nvarchar.
    // I will use 'string' for a direct mapping, but if it's meant to be a number, 
    // it should ideally be int/double in the database.
    public string MinimumPoints { get; set; }

    // Mapped from: MaximumPoints (int)
    public int MaximumPoints { get; set; }
}