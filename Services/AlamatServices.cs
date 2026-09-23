using Dapper;
using Katalog.Models;

namespace Katalog.Services
{
    public class AlamatServices
    {
        private readonly Database db;

        public AlamatServices(Database _db)
        {
            db = _db;
        }

        // =========================================================
        // GET ALL ALAMAT
        // ADMIN & PETUGAS
        // =========================================================
        public async Task<List<AlamatResponse>> GetAllAlamatAsync()
        {
            using var conn = db.connect();

            const string query = @"
                SELECT
                    a.id AS Id,
                    a.idUser AS IdUser,
                    u.Nama AS NamaUser,
                    a.no_telepon AS NoTelepon,
                    a.content AS Content
                FROM Alamat a
                INNER JOIN User u
                    ON u.Id = a.idUser
                WHERE a.deleted_at IS NULL
                ORDER BY a.id DESC;
            ";

            var result = await conn.QueryAsync<AlamatResponse>(query);

            return result.ToList();
        }

        // =========================================================
        // GET ALAMAT BY USER
        // ADMIN, PETUGAS & PELANGGAN
        //
        // idUser didapat dari Bearer Token di Controller
        // =========================================================
        public async Task<List<AlamatResponse>> GetAlamatByUserAsync(
            int idUser)
        {
            using var conn = db.connect();

            const string query = @"
                SELECT
                    a.id AS Id,
                    a.idUser AS IdUser,
                    u.Nama AS NamaUser,
                    a.no_telepon AS NoTelepon,
                    a.content AS Content
                FROM Alamat a
                INNER JOIN User u
                    ON u.Id = a.idUser
                WHERE a.idUser = @IdUser
                    AND a.deleted_at IS NULL
                ORDER BY a.id DESC;
            ";

            var result = await conn.QueryAsync<AlamatResponse>(
                query,
                new
                {
                    IdUser = idUser
                }
            );

            return result.ToList();
        }

        // =========================================================
        // GET ALAMAT BY ID
        // PELANGGAN
        //
        // id alamat harus milik user dari token
        // =========================================================
        public async Task<AlamatResponse?> GetAlamatByIdAsync(
            int id,
            int idUser)
        {
            using var conn = db.connect();

            const string query = @"
                SELECT
                    a.id AS Id,
                    a.idUser AS IdUser,
                    u.Nama AS NamaUser,
                    a.no_telepon AS NoTelepon,
                    a.content AS Content
                FROM Alamat a
                INNER JOIN User u
                    ON u.Id = a.idUser
                WHERE
                    a.id = @Id
                    AND a.idUser = @IdUser
                    AND a.deleted_at IS NULL
                LIMIT 1;
            ";

            return await conn.QueryFirstOrDefaultAsync<AlamatResponse>(
                query,
                new
                {
                    Id = id,
                    IdUser = idUser
                }
            );
        }

        // =========================================================
        // CREATE ALAMAT
        // PELANGGAN
        //
        // idUser didapat dari Bearer Token
        // =========================================================
        public async Task<AlamatResponse?> AddAlamatAsync(
            int idUser,
            AlamatRequest request)
        {
            using var conn = db.connect();

            const string query = @"
                INSERT INTO Alamat
                (
                    idUser,
                    content,
                    no_telepon
                )
                VALUES
                (
                    @IdUser,
                    @Content,
                    @NoTelepon
                );

                SELECT
                    a.id AS Id,
                    a.idUser AS IdUser,
                    u.Nama AS NamaUser,
                    a.no_telepon AS NoTelepon,
                    a.content AS Content
                FROM Alamat a
                INNER JOIN User u
                    ON u.Id = a.idUser
                WHERE a.id = LAST_INSERT_ID()
                    AND a.deleted_at IS NULL;
            ";

            return await conn.QueryFirstOrDefaultAsync<AlamatResponse>(
                query,
                new
                {
                    IdUser = idUser,
                    Content = request.Content,
                    NoTelepon = request.NoTelepon
                }
            );
        }

        // =========================================================
        // PATCH ALAMAT
        // PELANGGAN
        //
        // idUser didapat dari Bearer Token
        //
        // User hanya bisa mengubah alamat miliknya sendiri
        // =========================================================
        public async Task<bool> UpdateAlamatAsync(
            int id,
            int idUser,
            AlamatRequest request)
        {
            using var conn = db.connect();

            const string query = @"
                UPDATE Alamat
                SET
                    content = @Content,
                    no_telepon = @NoTelepon
                WHERE
                    id = @Id
                    AND idUser = @IdUser
                    AND deleted_at IS NULL;
            ";

            var result = await conn.ExecuteAsync(
                query,
                new
                {
                    Id = id,
                    IdUser = idUser,
                    Content = request.Content,
                    NoTelepon = request.NoTelepon
                }
            );

            return result > 0;
        }

        // =========================================================
        // DELETE ALAMAT (SOFT DELETE)
        // PELANGGAN
        //
        // idUser didapat dari Bearer Token
        //
        // User hanya bisa menghapus alamat miliknya sendiri.
        // Baris tidak dihapus permanen agar FK di Pesanan
        // tetap valid; deleted_at diisi sebagai penanda hapus.
        // =========================================================
        public async Task<bool> DeleteAlamatAsync(
            int id,
            int idUser)
        {
            using var conn = db.connect();

            const string query = @"
                UPDATE Alamat
                SET deleted_at = NOW()
                WHERE
                    id = @Id
                    AND idUser = @IdUser
                    AND deleted_at IS NULL;
            ";

            var result = await conn.ExecuteAsync(
                query,
                new
                {
                    Id = id,
                    IdUser = idUser
                }
            );

            return result > 0;
        }
    }
}
