using Dapper;
using Katalog.Models;

namespace Katalog.Services
{
    public class StatusProductServices
    {
        private readonly Database db;

        public StatusProductServices(Database _db)
        {
            db = _db;
        }
        public async Task<bool> CreateStatusProduct(StatusProduct statusProduct)
        {
            using var conn = db.connect();

            const string query = @"
                INSERT INTO status_product (nama)
                VALUES (@Nama)";

            var result = await conn.ExecuteAsync(query, statusProduct);

            return result > 0;
        }

        //DELETE
        public async Task<bool> DeleteStatusProduct(int id)
        {
            using var conn = db.connect();

            const string query = @"
                DELETE FROM status_product
                WHERE id = @Id";

            var result = await conn.ExecuteAsync(query, new { Id = id });

            return result > 0;
        }

        //Patch
        public async Task<bool> PatchStatusProduct(int id, StatusProduct statusProduct)
        {
            using var conn = db.connect();

            const string query = @"
                UPDATE status_product
                SET nama = @Nama
                WHERE id = @Id";

            var result = await conn.ExecuteAsync(query, new { Id = id, Nama = statusProduct.Nama });

            return result > 0;
        }

        //GET ALL STATUS PRODUCT
        public async Task<List<StatusProduct>> GetAllStatusProduct()
        {
            using var conn = db.connect();

            const string query = @"
                SELECT * FROM status_product";

            var result = await conn.QueryAsync<StatusProduct>(query);

            return result.ToList();
        }
    }
}
