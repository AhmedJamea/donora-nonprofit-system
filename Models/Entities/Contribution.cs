namespace Donora.Models.Entities
{
    public class Contribution
    {
        public int ReferenceId { get; set; } 
        public int SupporterId { get; set; } 
        public int InitiativeId { get; set; } 
        public decimal Amount { get; set; } 
        public DateTime Timestamp { get; set; }
    }
}
