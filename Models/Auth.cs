using Microsoft.AspNetCore.Authorization;

namespace Katalog.Models
{
    
    public static class Policies
    {
        public const string Admin = "Admin";
        public const string Petugas = "Petugas";
        public const string Pelanggan = "Pelanggan";

        public static void Register(AuthorizationOptions options)
        {
            options.AddPolicy(Admin, p => p.RequireRole("Admin"));
            options.AddPolicy(Petugas, p => p.RequireRole("Petugas"));
            options.AddPolicy(Pelanggan, p => p.RequireRole("Pelanggan"));
        }

    }
}
