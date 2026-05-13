namespace Donora.Models.Entities
{
    public class Expenditure
    {
        public int ExpId { get; set; }
        public int InitiativeId { get; set; } 
        public decimal AmountSpent { get; set; }
        public string VendorName { get; set; }
        public DateTime DateSpent { get; set; } // Added for simple reporting
    }
}
