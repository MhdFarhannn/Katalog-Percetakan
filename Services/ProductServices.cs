using Dapper;
using Katalog.Models;

namespace Katalog.Services
{
    public class ProductServices
    {
        private readonly Database db;

        public ProductServices(Database _db)
        {
            db = _db;
        }

        // MENAMBAHKAN PRODUCT
        public async Task<bool> AddProductAsync(Product product)
        {
            using var conn = db.connect();

            const string query = @"
                INSERT INTO product
                (
                    idKategoriProduct,
                    idStatusProduct,
                    nama,
                    deskripsi,
                    imagePath,
                    harga,
                    background_color
                )
                VALUES
                (
                    @IdKategoriProduct,
                    @IdStatusProduct,
                    @Nama,
                    @Deskripsi,
                    @ImagePath,
                    @Harga,
                    @BackgroundColor
                )";

            var result = await conn.ExecuteAsync(query, product);

            return result > 0;
        }

        // GET ALL PRODUCT
        public async Task<List<Product>> GetAllProductsAsync()
        {
            using var conn = db.connect();

            const string query = @"
                SELECT
                    id AS Id,
                    idKategoriProduct AS IdKategoriProduct,
                    idStatusProduct AS IdStatusProduct,
                    nama AS Nama,
                    deskripsi AS Deskripsi,
                    imagePath AS ImagePath,
                    harga AS Harga,
                    background_color AS BackgroundColor
                FROM product";

            var result = await conn.QueryAsync<Product>(query);

            return result.ToList();
        }

        // GET PRODUCT BY ID
        public async Task<Product?> GetProductByIdAsync(int id)
        {
            using var conn = db.connect();

            const string query = @"
                SELECT
                    id AS Id,
                    idKategoriProduct AS IdKategoriProduct,
                    idStatusProduct AS IdStatusProduct,
                    nama AS Nama,
                    deskripsi AS Deskripsi,
                    imagePath AS ImagePath,
                    harga AS Harga,
                    background_color AS BackgroundColor
                FROM product
                WHERE id = @Id";

            var result = await conn.QueryFirstOrDefaultAsync<Product>(
                query,
                new { Id = id }
            );

            return result;
        }

        // EDIT PRODUCT
        public async Task<bool> UpdateProductAsync(
            int id,
            Product product)
        {
            using var conn = db.connect();

            const string query = @"
                UPDATE product
                SET
                    idKategoriProduct = @IdKategoriProduct,
                    idStatusProduct = @IdStatusProduct,
                    nama = @Nama,
                    deskripsi = @Deskripsi,
                    imagePath = @ImagePath,
                    harga = @Harga,
                    background_color = @BackgroundColor
                WHERE id = @Id";

            var result = await conn.ExecuteAsync(query, new
            {
                Id = id,
                product.IdKategoriProduct,
                product.IdStatusProduct,
                product.Nama,
                product.Deskripsi,
                product.ImagePath,
                product.Harga,
                product.BackgroundColor
            });

            return result > 0;
        }

        // DELETE PRODUCT
        public async Task<bool> DeleteProductAsync(int id)
        {
            using var conn = db.connect();

            const string query = @"
                DELETE FROM product
                WHERE id = @Id";

            var result = await conn.ExecuteAsync(
                query,
                new { Id = id }
            );

            return result > 0;
        }
    }
}
