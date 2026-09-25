using Dapper;
using Katalog.Models;
using MySql.Data.MySqlClient;

namespace Katalog.Services
{
    // =========================================================
    // PRICING SERVICES
    //
    // Menjembatani database -> PricingCalculator.
    //   - Memuat konfigurasi harga product (+ varian Ukuran_Produk).
    //   - Memanggil PricingCalculator sebagai satu-satunya
    //     sumber rumus harga.
    //
    // Ada dua entry point:
    //   EvaluateAsync(input)                     -> buka koneksi sendiri
    //                                               (dipakai validasi controller)
    //   EvaluateAsync(conn, transaction, input)  -> ikut transaksi pesanan
    //                                               (dipakai PesananServices)
    // =========================================================
    public class PricingServices
    {
        private readonly Database db;

        public PricingServices(Database _db)
        {
            db = _db;
        }

        // Dipakai controller untuk memvalidasi item sebelum menulis
        // pesanan. Tidak membuka transaksi.
        public async Task<PricingResult?> EvaluateAsync(PricingInput input)
        {
            using var conn = db.connect();

            return await EvaluateAsync(conn, null, input);
        }

        // Dipakai PesananServices di dalam transaksi pesanan agar
        // rate yang dipakai konsisten dengan data yang dibaca.
        public static async Task<PricingResult?> EvaluateAsync(
            MySqlConnection conn,
            MySqlTransaction? transaction,
            PricingInput input)
        {
            var product = await conn.QueryFirstOrDefaultAsync<ProductPricingRow>(
                @"SELECT
                      p.harga AS Harga,
                      p.pricing_mode AS PricingMode,
                      p.dimension_unit AS DimensionUnit
                  FROM product p
                  WHERE p.id = @IdProduct
                      AND p.deleted_at IS NULL
                  LIMIT 1;",
                new { IdProduct = input.IdProduct },
                transaction);

            // Product tidak ada / sudah di-soft delete.
            if (product == null)
            {
                return null;
            }

            var config = new PricingConfiguration
            {
                Mode = PricingUnits.ParseMode(product.PricingMode),
                BasePrice = product.Harga,
                Unit = PricingUnits.ParseUnit(product.DimensionUnit)
            };

            if (input.IdUkuranProduk.HasValue)
            {
                var ukuran = await conn.QueryFirstOrDefaultAsync<UkuranPricingRow>(
                    @"SELECT
                          u.harga_tambahan AS HargaTambahan,
                          u.harga AS Harga,
                          u.panjang_cm AS PanjangCm,
                          u.lebar_cm AS LebarCm
                      FROM Ukuran_Produk u
                      WHERE u.id = @IdUkuranProduk
                          AND u.idProduct = @IdProduct
                      LIMIT 1;",
                    new
                    {
                        IdUkuranProduk = input.IdUkuranProduk.Value,
                        IdProduct = input.IdProduct
                    },
                    transaction);

                // Ukuran tidak valid untuk product ini.
                if (ukuran == null)
                {
                    return null;
                }

                config.AdditionalPrice = ukuran.HargaTambahan;
                config.VariantPrice = ukuran.Harga;
                config.PanjangCm = ukuran.PanjangCm;
                config.LebarCm = ukuran.LebarCm;
            }

            return PricingCalculator.Calculate(config, input);
        }

        // =========================================================
        // KATALOG VARIAN PRODUK
        // =========================================================
        public async Task<List<UkuranProduk>> GetUkuranByProductAsync(int idProduct)
        {
            using var conn = db.connect();

            const string query = @"
                SELECT
                    u.id AS Id,
                    u.idProduct AS IdProduct,
                    u.nama AS Nama,
                    u.harga_tambahan AS HargaTambahan,
                    u.harga AS Harga,
                    u.panjang_cm AS PanjangCm,
                    u.lebar_cm AS LebarCm
                FROM Ukuran_Produk u
                INNER JOIN product p
                    ON p.id = u.idProduct
                WHERE u.idProduct = @IdProduct
                    AND p.deleted_at IS NULL
                ORDER BY u.id;";

            var result = await conn.QueryAsync<UkuranProduk>(
                query,
                new { IdProduct = idProduct });

            return result.ToList();
        }
    }
}
