namespace Donora.Models.Entities
{
    public class Initiative
    {
        public int InitiativeId { get; set; } 
        public int SectorId { get; set; }
        public int CreatedByUserId { get; set; }
        public string InitiativeName { get; set; }
        public string Objective { get; set; } 
        public decimal FundingTarget { get; set; } 
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public decimal CurrentRaised { get; set; }
    }
}
