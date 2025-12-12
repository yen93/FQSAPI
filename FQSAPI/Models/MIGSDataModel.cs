using System;

public class MIGSDataModel
{
    // Mapped from: ClientID (float)
    public int ClientID { get; set; }

    // Mapped from: ClientName (nvarchar(255))
    public string ClientName { get; set; }

    // Mapped from: ClientAge (float)
    public double ClientAge { get; set; }

    // Mapped from: [Years Membership] (float) - Note: Using C# naming convention (PascalCase)
    public double YearsMembership { get; set; }

    // Mapped from: YearsMembershipPoints (float)
    public double YearsMembershipPoints { get; set; }

    // Mapped from: [Share Capital] (float) - Note: Using C# naming convention (PascalCase)
    public double ShareCapital { get; set; }

    // Mapped from: ShareCapPoints (float)
    public double ShareCapPoints { get; set; }

    // Mapped from: TotalADB (float)
    public double TotalADB { get; set; }

    // Mapped from: TotalADBPoints (float)
    public double TotalADBPoints { get; set; }

    // Mapped from: ShareCapDepositAYear (float)
    public double ShareCapDepositAYear { get; set; }

    // Mapped from: ShareCapDepositAYearPOINTS (float)
    public double ShareCapDepositAYearPOINTS { get; set; }

    // Mapped from: PAST3LoanPenaltyPaid (float)
    public double PAST3LoanPenaltyPaid { get; set; }

    // Mapped from: PAST3LoanPenaltyPaidPoints (float)
    public double PAST3LoanPenaltyPaidPoints { get; set; }

    // Mapped from: RECENTLOANPenaltyPaid (float)
    public double RECENTLOANPenaltyPaid { get; set; }

    // Mapped from: RECENTLOANPenaltyPaidPoints (float)
    public double RECENTLOANPenaltyPaidPoints { get; set; }

    // Mapped from: MIGSTotalPoints (float)
    public double MIGSTotalPoints { get; set; }

    // Mapped from: MIGSClassification (nvarchar(255))
    public string MIGSClassification { get; set; }
}