namespace FQSAPI.Models
{
    public class ContactUsFormModel
    {
        // Required
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        // Optional
        public string? Company { get; set; }
        public string? ServiceNeeded { get; set; }  // e.g. "CRM", "Custom Web App"
        public string? BudgetRange { get; set; }    // e.g. "50k–150k", "150k–300k"
        public string? Timeline { get; set; }       // e.g. "ASAP", "1–3 months"
        public string? PhoneNumber { get; set; }
        public string? PreferredContactMethod { get; set; }
        public string? Source { get; set; }         // e.g. "Facebook", "Referral"
    }
}
