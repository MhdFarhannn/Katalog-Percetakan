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
        // GET ALL PRODUCT
        public async Task<List<Product>> GetAllProductsAsync()
        {
            using var conn = db.connect();
        
            const string query = @"
                SELECT
                    p.id AS Id,
                    p.idKategoriProduct AS IdKategoriProduct,
                    p.idStatusProduct AS IdStatusProduct,
                    p.nama AS Nama,
                    p.deskripsi AS Deskripsi,
                    p.imagePath AS ImagePath,
                    p.harga AS Harga,
                    p.diskon AS Diskon,
                    p.background_color AS BackgroundColor,
        
                    kp.id AS Id,
                    kp.nama AS Nama,
        
                    sp.id AS Id,
                    sp.nama AS Nama
        
                FROM product p
        
                LEFT JOIN kategory_product kp
                    ON p.idKategoriProduct = kp.id
        
                LEFT JOIN status_product sp
                    ON p.idStatusProduct = sp.id

                WHERE p.deleted_at IS NULL
            ";
        
            var result = await conn.QueryAsync<
                Product,
                KategoryProduct,
                StatusProduct,
                Product
            >(
                query,
                (product, kategory, status) =>
                {
                    product.KategoryProduct = kategory;
                    product.StatusProduct = status;
        
                    return product;
                },
                splitOn: "Id,Id"
            );
        
            return result.ToList();
        }

        // // GET PRODUCT BY ID
        // public async Task<Product?> GetProductByIdAsync(int id)
        // {
        //     using var conn = db.connect();

        //     const string query = @"
        //         SELECT
        //             id AS Id,
        //             idKategoriProduct AS IdKategoriProduct,
        //             idStatusProduct AS IdStatusProduct,
        //             nama AS Nama,
        //             deskripsi AS Deskripsi,
        //             imagePath AS ImagePath,
        //             harga AS Harga,
        //             background_color AS BackgroundColor
        //         FROM product
        //         WHERE id = @Id";

        //     var result = await conn.QueryFirstOrDefaultAsync<Product>(
        //         query,
        //         new { Id = id }
        //     );

        //     return result;
        // }

        // EDIT PRODUCT
        public async Task<bool> UpdateProductAsync(int id, Product product)
        {
            using var conn = db.connect();
        
            const string query = @"
                UPDATE product
                SET
                    idKategoriProduct = COALESCE(@IdKategoriProduct, idKategoriProduct),
                    idStatusProduct   = COALESCE(@IdStatusProduct, idStatusProduct),
                    nama              = COALESCE(@Nama, nama),
                    deskripsi         = COALESCE(@Deskripsi, deskripsi),
                    imagePath         = COALESCE(@ImagePath, imagePath),
                    harga             = COALESCE(@Harga, harga),
                    background_color  = COALESCE(@BackgroundColor, background_color)
                WHERE id = @Id
                  AND deleted_at IS NULL";
        
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

        // DELETE PRODUCT (SOFT DELETE)
        // Baris tidak dihapus permanen; kolom deleted_at diisi
        // agar histori pesanan (FK RESTRICT) tetap valid.
        public async Task<bool> DeleteProductAsync(int id)
        {
            using var conn = db.connect();

            const string query = @"
                UPDATE product
                SET deleted_at = NOW()
                WHERE id = @Id
                    AND deleted_at IS NULL";

            var result = await conn.ExecuteAsync(
                query,
                new { Id = id }
            );

            return result > 0;
        }
    }
}
