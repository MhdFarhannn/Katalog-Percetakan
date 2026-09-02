using Microsoft.AspNetCore.Authorization;

namespace Katalog.Models
{
    
    public static class Policies
    {
        public const string Admin = "Admin";
        public const string Petugas = "Petugas";
        public const string Pelanggan = "Pelanggan";
        public const string AdminPetugasPelanggan = "AdminPetugasPelanggan";

        public static void Register(AuthorizationOptions options)
        {
            options.AddPolicy(Admin, p => p.RequireRole("Admin"));
            options.AddPolicy(Petugas, p => p.RequireRole("Petugas"));
            options.AddPolicy(Pelanggan, p => p.RequireRole("Pelanggan"));
            options.AddPolicy(AdminPetugasPelanggan, p => p.RequireRole("Admin", "Petugas", "Pelanggan"));
        }

    }

        public class AuthMeResponse
        {
            public int Id { get; set; }
    
            public string Nama { get; set; } = string.Empty;
    
            public int? IdRole { get; set; }
    
            public string? Role { get; set; }
    
            public string? Email { get; set; }
    
            public string AuthProvider { get; set; } = string.Empty;
    
            public bool IsActive { get; set; }
    
            public string? NoTelepon { get; set; }
        }

}
