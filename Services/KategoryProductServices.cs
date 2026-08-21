using Dapper;
using Katalog.Models;

namespace Katalog.Services
{
    public class KategoryProductServices
    {
        private readonly Database db;

        public KategoryProductServices(Database _db)
        {
            db = _db;
        }
        public async Task<bool> CreateKategoryProduct(KategoryProduct categoryProduct)
        {
            using var conn = db.connect();

            const string query = @"
                INSERT INTO kategory_product (nama)
                VALUES (@Nama)";

            var result = await conn.ExecuteAsync(query, categoryProduct);

            return result > 0;
        }

        //DELETE
        public async Task<bool> DeleteKategoryProduct(int id)
        {
            using var conn = db.connect();

            const string query = @"
                DELETE FROM kategory_product
                WHERE id = @Id";

            var result = await conn.ExecuteAsync(query, new { Id = id });

            return result > 0;
        }

        //Patch
        public async Task<bool> PatchKategoryProduct(int id, KategoryProduct categoryProduct)
        {
            using var conn = db.connect();

            const string query = @"
                UPDATE kategory_product
                SET nama = @Nama
                WHERE id = @Id";

            var result = await conn.ExecuteAsync(query, new { Id = id, Nama = categoryProduct.Nama });

            return result > 0;
        }

        //GET ALL KATEGORI PRODUCT
        public async Task<List<KategoryProduct>> GetAllKategoryProduct()
        {
            using var conn = db.connect();

            const string query = @"
                SELECT * FROM kategory_product";

            var result = await conn.QueryAsync<KategoryProduct>(query);

            return result.ToList();
        }
    }
}
