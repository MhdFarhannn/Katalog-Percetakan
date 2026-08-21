namespace Katalog.Models
{
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }


    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Nama { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
