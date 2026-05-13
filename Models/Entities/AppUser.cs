namespace Donora.Models.Entities
{
    public class AppUser
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; } // "Admin" or "Supporter"
    }
}
