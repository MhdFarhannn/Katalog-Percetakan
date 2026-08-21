using Dapper;
using Katalog.Models;

namespace Katalog.Services
{
    public class StatusPengerjaanServices
    {
        private readonly Database db;

        public StatusPengerjaanServices(Database _db)
        {
            db = _db;
        }
        public async Task<bool> CreateStatusPengerjaan(StatusPengerjaan statusPengerjaan)
        {
            using var conn = db.connect();

            const string query = @"
                INSERT INTO status_pengerjaan (nama)
                VALUES (@Nama)";

            var result = await conn.ExecuteAsync(query, statusPengerjaan);

            return result > 0;
        }

        //DELETE
        public async Task<bool> DeleteStatusPengerjaan(int id)
        {
            using var conn = db.connect();

            const string query = @"
                DELETE FROM status_pengerjaan   
                WHERE id = @Id";

            var result = await conn.ExecuteAsync(query, new { Id = id });

            return result > 0;
        }

        //Patch
        public async Task<bool> PatchStatusPengerjaan(int id, StatusPengerjaan statusPengerjaan)
        {
            using var conn = db.connect();

            const string query = @"
                UPDATE status_pengerjaan
                SET nama = @Nama
                WHERE id = @Id";

            var result = await conn.ExecuteAsync(query, new { Id = id, Nama = statusPengerjaan.Nama });

            return result > 0;
        }

        //GET ALL STATUS PENGERJAAN
        public async Task<List<StatusPengerjaan>> GetAllStatusPengerjaan()
        {
            using var conn = db.connect();

            const string query = @"
                SELECT * FROM status_pengerjaan";

            var result = await conn.QueryAsync<StatusPengerjaan>(query);

            return result.ToList();
        }
    }
}
