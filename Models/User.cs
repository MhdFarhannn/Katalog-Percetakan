namespace Katalog.Models
{

    public class User
    {
        public string Role { get; set; } = string.Empty;
        public int Id { get; set; }
        public string Nama { get; set; } = string.Empty;
        public string Email { get; set; }= string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Auth_Provider { get; set; } = "local";
        public string External_Id { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime? RefreshTokenExpired { get; set; }
        public bool Is_Active { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class UserCreate
    {
        public string Nama { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int Id_Role { get; set; }
        public bool Is_Active { get; set; }
        public string Email { get; set; } = string.Empty;
        public string External_Id {get;set;} = string.Empty;
    }
}

