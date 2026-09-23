using Dapper;
using Katalog.Models;

namespace Katalog.Services
{
    public class PesananServices
    {
        private readonly Database db;
        private readonly PaymentServices payment;

        public PesananServices(
            Database _db,
            PaymentServices _payment)
        {
            db = _db;
            payment = _payment;
        }

        private const string SelectPesanan = @"
            SELECT
                ps.id AS Id,
                ps.idUser AS IdUser,
                u.Nama AS NamaUser,
                ps.idAlamat AS IdAlamat,
                a.content AS Alamat,
                ps.idStatusPengerjaan AS IdStatusPengerjaan,
                sp.nama AS StatusPengerjaan,
                ps.total_harga AS TotalHarga,
                (
                    -- idStatusPayment pembayaran terakhir pada payments
                    -- (JOIN status_payment), kode status dipetakan di C#
                    SELECT p.idStatusPayment
                    FROM payments p
                    INNER JOIN status_payment spm
                        ON spm.id = p.idStatusPayment
                    WHERE p.idPesanan = ps.id
                    ORDER BY p.id DESC
                    LIMIT 1
                ) AS IdStatusPayment,
                ps.Created_At AS CreatedAt
            FROM Pesanan ps
            INNER JOIN User u
                ON u.Id = ps.idUser
            INNER JOIN Alamat a
                ON a.id = ps.idAlamat
            INNER JOIN status_pengerjaan sp
                ON sp.id = ps.idStatusPengerjaan
        ";

        private const string SelectDetail = @"
            SELECT
                d.id AS Id,
                d.idPesanan AS IdPesanan,
                d.idProduct AS IdProduct,
                pr.nama AS NamaProduct,
                d.idUkuranProduk AS IdUkuranProduk,
                up.nama AS NamaUkuran,
                d.ukuran_custom AS UkuranCustom,
                d.qty AS Qty,
                d.harga_satuan AS HargaSatuan,
                d.notes AS Notes,
                d.desain_file_path AS DesainFilePath,
                d.desain_text AS DesainText
            FROM Pesanan_Detail d
            INNER JOIN product pr
                ON pr.id = d.idProduct
            LEFT JOIN Ukuran_Produk up
                ON up.id = d.idUkuranProduk
        ";

        // =========================================================
        // GET ALL PESANAN
        // ADMIN & PETUGAS
        // =========================================================
        public async Task<List<PesananResponse>> GetAllPesananAsync()
        {
            using var conn = db.connect();

            var result = (await conn.QueryAsync<PesananResponse>(
                SelectPesanan + " ORDER BY ps.id DESC;"))
                .ToList();

            await AttachDetailsAsync(conn, result);

            ApplyPaymentStatus(result);

            await SyncPendingPaymentsAsync(result);

            return result;
        }

        // =========================================================
        // GET PESANAN BY USER
        // idUser didapat dari Bearer Token
        // =========================================================
        public async Task<List<PesananResponse>> GetPesananByUserAsync(
            int idUser)
        {
            using var conn = db.connect();

            var result = (await conn.QueryAsync<PesananResponse>(
                SelectPesanan
                    + " WHERE ps.idUser = @IdUser ORDER BY ps.id DESC;",
                new { IdUser = idUser }))
                .ToList();

            await AttachDetailsAsync(conn, result);

            ApplyPaymentStatus(result);

            await SyncPendingPaymentsAsync(result);

            return result;
        }

        // =========================================================
        // GET PESANAN BY ID
        //
        // idUser null = akses Admin/Petugas (tanpa filter pemilik)
        // =========================================================
        public async Task<PesananResponse?> GetPesananByIdAsync(
            int id,
            int? idUser)
        {
            using var conn = db.connect();

            var query = SelectPesanan + " WHERE ps.id = @Id";

            if (idUser.HasValue)
            {
                query += " AND ps.idUser = @IdUser";
            }

            query += " LIMIT 1;";

            var result =
                await conn.QueryFirstOrDefaultAsync<PesananResponse>(
                    query,
                    new
                    {
                        Id = id,
                        IdUser = idUser
                    });

            if (result == null)
            {
                return null;
            }

            ApplyPaymentStatus(result);

            await SyncPendingPaymentsAsync(
                new List<PesananResponse> { result });

            result.Details = (await conn.QueryAsync<PesananDetailResponse>(
                SelectDetail + " WHERE d.idPesanan = @Id;",
                new { Id = id }))
                .ToList();

            return result;
        }

        // =========================================================
        // VALIDASI RELASI
        // =========================================================
        public async Task<bool> AlamatExistsAsync(
            int idAlamat,
            int idUser)
        {
            using var conn = db.connect();

            const string query = @"
                SELECT COUNT(*)
                FROM Alamat
                WHERE id = @IdAlamat AND idUser = @IdUser;";

            var count = await conn.ExecuteScalarAsync<int>(
                query,
                new
                {
                    IdAlamat = idAlamat,
                    IdUser = idUser
                });

            return count > 0;
        }

        public async Task<bool> ProductExistsAsync(int idProduct)
        {
            using var conn = db.connect();

            const string query = @"
                SELECT COUNT(*)
                FROM product
                WHERE id = @IdProduct;";

            var count = await conn.ExecuteScalarAsync<int>(
                query,
                new { IdProduct = idProduct });

            return count > 0;
        }

        public async Task<bool> UkuranBelongsToProductAsync(
            int idUkuranProduk,
            int idProduct)
        {
            using var conn = db.connect();

            const string query = @"
                SELECT COUNT(*)
                FROM Ukuran_Produk
                WHERE id = @IdUkuranProduk
                    AND idProduct = @IdProduct;";

            var count = await conn.ExecuteScalarAsync<int>(
                query,
                new
                {
                    IdUkuranProduk = idUkuranProduk,
                    IdProduct = idProduct
                });

            return count > 0;
        }

        // =========================================================
        // CEK APAKAH PESANAN SUDAH MEMILIKI PEMBAYARAN
        // =========================================================
        public async Task<bool> HasPaymentAsync(int idPesanan)
        {
            using var conn = db.connect();

            const string query = @"
                SELECT COUNT(*)
                FROM payments
                WHERE idPesanan = @IdPesanan;";

            var count = await conn.ExecuteScalarAsync<int>(
                query,
                new { IdPesanan = idPesanan });

            return count > 0;
        }

        // =========================================================
        // CREATE PESANAN + DETAIL
        //
        // idUser didapat dari Bearer Token.
        // total_harga dihitung di server.
        // =========================================================
        public async Task<PesananResponse?> CreatePesananAsync(
            int idUser,
            PesananRequest request)
        {
            using var conn = db.connect();

            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();

            var idStatus = await conn.ExecuteScalarAsync<int?>(
                "SELECT id FROM status_pengerjaan ORDER BY id LIMIT 1;",
                transaction: transaction);

            if (idStatus == null)
            {
                transaction.Rollback();
                return null;
            }

            var details = new List<PesananDetailRequest>();
            decimal totalHarga = 0;

            foreach (var item in request.Items)
            {
                var harga = await GetHargaSatuanAsync(
                    conn,
                    transaction,
                    item);

                if (harga == null)
                {
                    transaction.Rollback();
                    return null;
                }

                totalHarga += harga.Value * item.Qty;

                details.Add(new PesananDetailRequest
                {
                    IdProduct = item.IdProduct,
                    IdUkuranProduk = item.IdUkuranProduk,
                    UkuranCustom = item.UkuranCustom,
                    Qty = item.Qty,
                    Notes = item.Notes,
                    DesainFilePath = item.DesainFilePath,
                    DesainText = item.DesainText
                });
            }

            const string insertPesanan = @"
                INSERT INTO Pesanan
                (
                    idUser,
                    idAlamat,
                    idStatusPengerjaan,
                    total_harga
                )
                VALUES
                (
                    @IdUser,
                    @IdAlamat,
                    @IdStatusPengerjaan,
                    @TotalHarga
                );

                SELECT LAST_INSERT_ID();";

            var idPesanan = await conn.ExecuteScalarAsync<int>(
                insertPesanan,
                new
                {
                    IdUser = idUser,
                    IdAlamat = request.IdAlamat,
                    IdStatusPengerjaan = idStatus.Value,
                    TotalHarga = totalHarga
                },
                transaction);

            await InsertDetailsAsync(
                conn,
                transaction,
                idPesanan,
                details);

            transaction.Commit();

            return await GetPesananByIdAsync(idPesanan, idUser);
        }

        // =========================================================
        // UPDATE PESANAN + DETAIL
        //
        // Hanya pemilik dan hanya jika belum ada pembayaran.
        // =========================================================
        public async Task<PesananResponse?> UpdatePesananAsync(
            int id,
            int idUser,
            PesananRequest request)
        {
            using var conn = db.connect();

            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();

            const string ownerQuery = @"
                SELECT id
                FROM Pesanan
                WHERE id = @Id AND idUser = @IdUser
                LIMIT 1;";

            var ownerId = await conn.QueryFirstOrDefaultAsync<int?>(
                ownerQuery,
                new { Id = id, IdUser = idUser },
                transaction);

            if (ownerId == null)
            {
                transaction.Rollback();
                return null;
            }

            var paymentCount = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM payments WHERE idPesanan = @Id;",
                new { Id = id },
                transaction);

            if (paymentCount > 0)
            {
                transaction.Rollback();
                return null;
            }

            var details = new List<PesananDetailRequest>();
            decimal totalHarga = 0;

            foreach (var item in request.Items)
            {
                var harga = await GetHargaSatuanAsync(
                    conn,
                    transaction,
                    item);

                if (harga == null)
                {
                    transaction.Rollback();
                    return null;
                }

                totalHarga += harga.Value * item.Qty;

                details.Add(new PesananDetailRequest
                {
                    IdProduct = item.IdProduct,
                    IdUkuranProduk = item.IdUkuranProduk,
                    UkuranCustom = item.UkuranCustom,
                    Qty = item.Qty,
                    Notes = item.Notes,
                    DesainFilePath = item.DesainFilePath,
                    DesainText = item.DesainText
                });
            }

            await conn.ExecuteAsync(
                @"UPDATE Pesanan
                  SET idAlamat = @IdAlamat,
                      total_harga = @TotalHarga
                  WHERE id = @Id;",
                new
                {
                    Id = id,
                    IdAlamat = request.IdAlamat,
                    TotalHarga = totalHarga
                },
                transaction);

            await conn.ExecuteAsync(
                "DELETE FROM Pesanan_Detail WHERE idPesanan = @Id;",
                new { Id = id },
                transaction);

            await InsertDetailsAsync(
                conn,
                transaction,
                id,
                details);

            transaction.Commit();

            return await GetPesananByIdAsync(id, idUser);
        }

        // =========================================================
        // DELETE PESANAN + DETAIL
        //
        // Hanya pemilik dan hanya jika belum ada pembayaran.
        // =========================================================
        public async Task<bool> DeletePesananAsync(
            int id,
            int idUser)
        {
            using var conn = db.connect();

            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();

            var ownerId = await conn.QueryFirstOrDefaultAsync<int?>(
                @"SELECT id FROM Pesanan
                  WHERE id = @Id AND idUser = @IdUser
                  LIMIT 1;",
                new { Id = id, IdUser = idUser },
                transaction);

            if (ownerId == null)
            {
                transaction.Rollback();
                return false;
            }

            var paymentCount = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM payments WHERE idPesanan = @Id;",
                new { Id = id },
                transaction);

            if (paymentCount > 0)
            {
                transaction.Rollback();
                return false;
            }

            await conn.ExecuteAsync(
                "DELETE FROM Pesanan_Detail WHERE idPesanan = @Id;",
                new { Id = id },
                transaction);

            await conn.ExecuteAsync(
                "DELETE FROM Pesanan WHERE id = @Id;",
                new { Id = id },
                transaction);

            transaction.Commit();

            return true;
        }

        // =========================================================
        // HELPER
        // =========================================================
        private static async Task<decimal?> GetHargaSatuanAsync(
            MySql.Data.MySqlClient.MySqlConnection conn,
            MySql.Data.MySqlClient.MySqlTransaction transaction,
            PesananDetailRequest item)
        {
            var harga = await conn.QueryFirstOrDefaultAsync<decimal?>(
                "SELECT harga FROM product WHERE id = @IdProduct LIMIT 1;",
                new { IdProduct = item.IdProduct },
                transaction);

            if (harga == null)
            {
                return null;
            }

            var hargaSatuan = harga.Value;

            if (item.IdUkuranProduk.HasValue)
            {
                var tambahan =
                    await conn.QueryFirstOrDefaultAsync<decimal?>(
                        @"SELECT harga_tambahan
                          FROM Ukuran_Produk
                          WHERE id = @IdUkuranProduk
                              AND idProduct = @IdProduct
                          LIMIT 1;",
                        new
                        {
                            IdUkuranProduk = item.IdUkuranProduk,
                            IdProduct = item.IdProduct
                        },
                        transaction);

                // Ukuran tidak valid untuk product ini
                if (tambahan == null)
                {
                    return null;
                }

                hargaSatuan += tambahan.Value;
            }

            return hargaSatuan;
        }

        private static async Task InsertDetailsAsync(
            MySql.Data.MySqlClient.MySqlConnection conn,
            MySql.Data.MySqlClient.MySqlTransaction transaction,
            int idPesanan,
            List<PesananDetailRequest> details)
        {
            const string insertDetail = @"
                INSERT INTO Pesanan_Detail
                (
                    idPesanan,
                    idProduct,
                    idUkuranProduk,
                    ukuran_custom,
                    qty,
                    harga_satuan,
                    notes,
                    desain_file_path,
                    desain_text
                )
                VALUES
                (
                    @IdPesanan,
                    @IdProduct,
                    @IdUkuranProduk,
                    @UkuranCustom,
                    @Qty,
                    @HargaSatuan,
                    @Notes,
                    @DesainFilePath,
                    @DesainText
                );";

            foreach (var item in details)
            {
                var harga = await GetHargaSatuanAsync(
                    conn,
                    transaction,
                    item);

                await conn.ExecuteAsync(
                    insertDetail,
                    new
                    {
                        IdPesanan = idPesanan,
                        IdProduct = item.IdProduct,
                        IdUkuranProduk = item.IdUkuranProduk,
                        UkuranCustom = item.UkuranCustom,
                        Qty = item.Qty,
                        HargaSatuan = harga ?? 0,
                        Notes = item.Notes,
                        DesainFilePath = item.DesainFilePath,
                        DesainText = item.DesainText
                    },
                    transaction);
            }
        }

        // =========================================================
        // PEMETAAN STATUS PEMBAYARAN
        //
        // idStatusPayment didapat dari payments (JOIN status_payment).
        // Kode status ('pending', 'paid', ...) dipetakan di C# lewat
        // PaymentStatusMap. Pesanan yang belum punya baris di payments
        // tetap bernilai 'unpaid' (default PesananResponse).
        // =========================================================
        private static void ApplyPaymentStatus(PesananResponse pesanan)
        {
            if (pesanan.IdStatusPayment.HasValue)
            {
                pesanan.PaymentStatus = PaymentStatusMap.ToCode(
                    pesanan.IdStatusPayment.Value);
            }
        }

        private static void ApplyPaymentStatus(
            List<PesananResponse> pesananList)
        {
            foreach (var pesanan in pesananList)
            {
                ApplyPaymentStatus(pesanan);
            }
        }

        // =========================================================
        // SINKRONISASI STATUS PEMBAYARAN
        //
        // Frontend memantau status pembayaran lewat daftar / detail
        // pesanan, jadi pesanan yang pembayarannya masih 'pending'
        // ditarik status terakhirnya dari Midtrans. Pesanan dengan
        // pembayaran final (paid / cancelled / expired / ...) tidak
        // perlu disinkronkan lagi.
        // =========================================================
        private async Task SyncPendingPaymentsAsync(
            List<PesananResponse> pesananList)
        {
            var ids = pesananList
                .Where(p =>
                    p.IdStatusPayment
                        == PaymentStatusMap.MenungguPembayaran)
                .Select(p => p.Id)
                .ToList();

            if (ids.Count == 0)
            {
                return;
            }

            var synced = await payment.SyncPendingPaymentsAsync(ids);

            if (synced.Count == 0)
            {
                return;
            }

            foreach (var pesanan in pesananList)
            {
                if (synced.TryGetValue(
                    pesanan.Id,
                    out var idStatusPayment))
                {
                    pesanan.IdStatusPayment = idStatusPayment;
                }
            }

            ApplyPaymentStatus(pesananList);
        }

        private static async Task AttachDetailsAsync(
            MySql.Data.MySqlClient.MySqlConnection conn,
            List<PesananResponse> pesananList)
        {
            if (pesananList.Count == 0)
            {
                return;
            }

            var ids = pesananList.Select(p => p.Id).ToList();

            var details = (await conn.QueryAsync<PesananDetailResponse>(
                SelectDetail + " WHERE d.idPesanan IN @Ids;",
                new { Ids = ids }))
                .ToList();

            var grouped = details
                .GroupBy(d => d.IdPesanan)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var pesanan in pesananList)
            {
                if (grouped.TryGetValue(pesanan.Id, out var list))
                {
                    pesanan.Details = list;
                }
            }
        }
    }
}
