using Dapper;
using Katalog.Models;

namespace Katalog.Services
{
    public class AuthServices
    {
        private readonly Database db;

        public AuthServices(Database _db)
        {
            db = _db;
        }

        public async Task<bool> RegisterAdmin(UserCreate data)
        {
            using var conn = db.connect();
            string sql = @"INSERT INTO User(Nama,Email,Id_Role,Password) VALUES(@Nama,@Email,@Id_Role,@Password)";

            return await conn.ExecuteAsync(sql, new
            {
                Nama = data.Nama,
                Email = data.Email,
                Id_Role = 1,
                Password = data.Password,
                Is_Active = true
            }) > 0;
        }


        public async Task<bool> AdminIsRegistered()
        {
            using var conn = db.connect();
            var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM User");
            return count > 0;
        }

        public async Task<User?> LoginOrRegisterGoogle(string externalId, string email, string nama)
        {
            using var conn = db.connect();

            var sqlFind = @"
        SELECT
            u.Id,
            u.Nama,
            u.Email,
            u.Password,
            u.Auth_Provider,
            u.External_Id,
            u.Is_Active,
            r.Nama AS Role
        FROM User u
        JOIN Roles r ON u.Id_Role = r.Id
        WHERE u.Auth_Provider = 'google' AND u.External_Id = @ExternalId;";

            var existing = await conn.QueryFirstOrDefaultAsync<User>(sqlFind, new { ExternalId = externalId });
            if (existing != null)
            {
                return existing;
            }

            var sqlInsert = @"
        INSERT INTO User(Nama, Id_Role, Email, Password, Auth_Provider, External_Id, Is_Active)
        VALUES(@Nama, @Id_Role, @Email, NULL, 'google', @ExternalId, 1);";

            await conn.ExecuteAsync(sqlInsert, new
            {
                Nama = nama,
                Id_Role = 3, // Pelanggan
                Email = email,
                ExternalId = externalId
            });

            return await conn.QueryFirstOrDefaultAsync<User>(sqlFind, new { ExternalId = externalId });
        }

        public async Task UpdateRefreshToken(string RefreshToken, DateTime Expired, int user_id)
        {
            using var conn = db.connect();
            var sql = @"UPDATE User SET Refresh_Token = @RefreshToken,Refresh_Token_Expired=@Expired WHERE Id=@user_id";
            await conn.ExecuteAsync(sql, new { RefreshToken = RefreshToken, Expired = Expired, user_id = user_id });
        }

        public async Task<User?> Login(LoginRequest data)
        {
            using var conn = db.connect();
            var sql = @"
        SELECT
            u.Id,
            u.Nama,
            u.Email,
            u.Password,
            u.Is_Active,
            r.Nama AS Role
        FROM User u
        JOIN Roles r ON u.Id_Role = r.Id
        WHERE u.Email = @Email;";
            return await conn.QueryFirstOrDefaultAsync<User>(sql, data);
        }



            // GET CURRENT USER
            public async Task<AuthMeResponse?> GetMeAsync(int idUser)
            {
                using var conn = db.connect();

                const string query = @"
                        SELECT
                            u.Id AS Id,
                            u.Nama AS Nama,
                            u.Id_Role AS IdRole,
                            r.Nama AS Role,
                            u.Email AS Email,
                            u.Auth_Provider AS AuthProvider,
                            u.Is_Active AS IsActive
                        FROM User u
                        LEFT JOIN Roles r
                            ON u.Id_Role = r.Id
                        WHERE u.Id = @IdUser
                        LIMIT 1
                    ";

                var result = await conn.QueryFirstOrDefaultAsync<AuthMeResponse>(
                    query,
                    new
                    {
                        IdUser = idUser
                    }
                );

                return result;
            }
        
    

    }//Class
}//Namespace
