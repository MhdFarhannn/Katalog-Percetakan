
namespace Katalog.Services
{
    public interface IPasswordService
    {
        string HashPassword(string Password);
        bool VerifyPassword(string Password, string HashedPassword);
    }

    public class PasswordService : IPasswordService
    {
        public string HashPassword(string Password)
        {
            return BCrypt.Net.BCrypt.HashPassword(Password);
        }

        public bool VerifyPassword(string Password, string HashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(Password, HashedPassword);
        }
 
    }
}
