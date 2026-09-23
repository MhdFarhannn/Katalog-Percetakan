using System.Globalization;
using Dapper;
using Katalog.Models;

namespace Katalog.Services
{
    public class PaymentServices
    {
        private readonly Database db;
        private readonly MidtransService midtrans;
        private readonly ILogger<PaymentServices> logger;

        public PaymentServices(
            Database _db,
            MidtransService _midtrans,
            ILogger<PaymentServices> _logger)
        {
            db = _db;
            midtrans = _midtrans;
            logger = _logger;
        }

        // Nama status_pengerjaan saat pembayaran dibatalkan
        private const string PesananDibatalkan = "Dibatalkan";

        // =========================================================
        // CREATE PAYMENT (SNAP TOKEN)
        //
        // idUser didapat dari Bearer Token.
        // Pesanan harus milik user dan belum dibayar.
        // =========================================================
        public async Task<PaymentResult> CreatePaymentAsync(
            int idPesanan,
            int idUser)
        {
            using var conn = db.connect();

            const string pesananQuery = @"
                SELECT
                    ps.id AS Id,
                    ps.idUser AS IdUser,
                    ps.total_harga AS TotalHarga,
                    sp.nama AS StatusPengerjaan,
                    u.Nama AS NamaUser,
                    u.Email AS Email
                FROM Pesanan ps
                INNER JOIN User u
                    ON u.Id = ps.idUser
                INNER JOIN status_pengerjaan sp
                    ON sp.id = ps.idStatusPengerjaan
                WHERE
                    ps.id = @IdPesanan
                    AND ps.idUser = @IdUser
                LIMIT 1;";

            var pesanan =
                await conn.QueryFirstOrDefaultAsync<PesananPaymentInfo>(
                    pesananQuery,
                    new
                    {
                        IdPesanan = idPesanan,
                        IdUser = idUser
                    });

            if (pesanan == null)
            {
                return PaymentResult.Fail(
                    "Pesanan tidak ditemukan");
            }

            // Pesanan yang sudah dibatalkan tidak dapat dibayar
            if (string.Equals(
                pesanan.StatusPengerjaan,
                PesananDibatalkan,
                StringComparison.OrdinalIgnoreCase))
            {
                return PaymentResult.Fail(
                    "Pesanan sudah dibatalkan");
            }

            // Idempotent: kembalikan Snap token yang masih pending
            const string existingQuery = @"
                SELECT
                    id AS Id,
                    idPesanan AS IdPesanan,
                    idStatusPayment AS IdStatusPayment,
                    midtrans_order_id AS MidtransOrderId,
                    midtrans_transaction_id AS MidtransTransactionId,
                    snap_token AS SnapToken,
                    payment_type AS PaymentType,
                    gross_amount AS GrossAmount,
                    transaction_status AS TransactionStatus,
                    transaction_time AS TransactionTime,
                    settlement_time AS SettlementTime,
                    expiry_time AS ExpiryTime
                FROM payments
                WHERE
                    idPesanan = @IdPesanan
                    AND idStatusPayment = @IdStatusPayment
                    AND snap_token IS NOT NULL
                ORDER BY id DESC
                LIMIT 1;";

            var existing =
                await conn.QueryFirstOrDefaultAsync<Payment>(
                    existingQuery,
                    new
                    {
                        IdPesanan = idPesanan,
                        IdStatusPayment =
                            PaymentStatusMap.MenungguPembayaran
                    });

            if (existing != null)
            {
                return PaymentResult.Ok(
                    BuildPaymentResponse(existing),
                    "Pembayaran sudah dibuat");
            }

            var paidCount = await conn.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*) FROM payments
                  WHERE idPesanan = @IdPesanan
                      AND idStatusPayment = @IdStatusPayment;",
                new
                {
                    IdPesanan = idPesanan,
                    IdStatusPayment = PaymentStatusMap.Dibayar
                });

            if (paidCount > 0)
            {
                return PaymentResult.Fail("Pesanan sudah dibayar");
            }

            var details = (await conn.QueryAsync<PesananDetailResponse>(
                @"SELECT
                    d.idProduct AS IdProduct,
                    pr.nama AS NamaProduct,
                    d.qty AS Qty,
                    d.harga_satuan AS HargaSatuan
                  FROM Pesanan_Detail d
                  INNER JOIN product pr
                      ON pr.id = d.idProduct
                  WHERE d.idPesanan = @IdPesanan;",
                new { IdPesanan = idPesanan }))
                .ToList();

            var midtransOrderId =
                $"PESANAN-{pesanan.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}";

            var snapRequest = new MidtransSnapRequest
            {
                TransactionDetails = new MidtransTransactionDetails
                {
                    OrderId = midtransOrderId,
                    GrossAmount = pesanan.TotalHarga
                },
                CustomerDetails = new MidtransCustomerDetails
                {
                    FirstName = pesanan.NamaUser,
                    Email = pesanan.Email
                }
            };

            if (details.Count > 0)
            {
                snapRequest.ItemDetails = details
                    .Select(d => new MidtransItemDetails
                    {
                        Id = d.IdProduct.ToString(),
                        Price = d.HargaSatuan,
                        Quantity = d.Qty,
                        Name = d.NamaProduct
                    })
                    .ToList();
            }

            // Bisa melempar MidtransException
            var snap =
                await midtrans.CreateSnapTransactionAsync(snapRequest);

            const string insertQuery = @"
                INSERT INTO payments
                (
                    idPesanan,
                    idStatusPayment,
                    midtrans_order_id,
                    snap_token,
                    gross_amount
                )
                VALUES
                (
                    @IdPesanan,
                    @IdStatusPayment,
                    @MidtransOrderId,
                    @SnapToken,
                    @GrossAmount
                );

                SELECT LAST_INSERT_ID();";

            await conn.ExecuteScalarAsync<int>(
                insertQuery,
                new
                {
                    IdPesanan = pesanan.Id,
                    IdStatusPayment =
                        PaymentStatusMap.MenungguPembayaran,
                    MidtransOrderId = midtransOrderId,
                    SnapToken = snap.Token,
                    GrossAmount = pesanan.TotalHarga
                });

            logger.LogInformation(
                "Payment Snap dibuat untuk Pesanan {IdPesanan}",
                pesanan.Id);

            return PaymentResult.Ok(new PaymentResponse
            {
                IdPesanan = pesanan.Id,
                MidtransOrderId = midtransOrderId,
                GrossAmount = pesanan.TotalHarga,
                SnapToken = snap.Token,
                RedirectUrl = snap.RedirectUrl,
                PaymentStatus = PaymentStatusMap.Pending
            });
        }

        // =========================================================
        // GET PAYMENT BY PESANAN
        //
        // idUser didapat dari Bearer Token.
        // =========================================================
        public async Task<PaymentResponse?> GetPaymentByPesananAsync(
            int idPesanan,
            int idUser)
        {
            using var conn = db.connect();

            const string query = @"
                SELECT
                    p.id AS Id,
                    p.idPesanan AS IdPesanan,
                    p.idStatusPayment AS IdStatusPayment,
                    p.midtrans_order_id AS MidtransOrderId,
                    p.gross_amount AS GrossAmount,
                    p.snap_token AS SnapToken
                FROM payments p
                INNER JOIN Pesanan ps
                    ON ps.id = p.idPesanan
                WHERE
                    p.idPesanan = @IdPesanan
                    AND ps.idUser = @IdUser
                ORDER BY p.id DESC
                LIMIT 1;";

            var payment =
                await conn.QueryFirstOrDefaultAsync<Payment>(
                    query,
                    new
                    {
                        IdPesanan = idPesanan,
                        IdUser = idUser
                    });

            if (payment == null)
            {
                return null;
            }

            // Endpoint ini di-polling frontend sampai pembayaran lunas,
            // jadi status terakhir ikut disinkronkan dari Midtrans.
            // Dengan begitu status tetap berubah walau notifikasi
            // webhook belum / tidak sampai ke server.
            await SyncPaymentStatusAsync(conn, payment);

            return BuildPaymentResponse(payment);
        }

        // =========================================================
        // SINKRONISASI STATUS PEMBAYARAN DARI MIDTRANS
        //
        // Hanya diproses untuk pembayaran yang belum final (masih
        // MENUNGGU PEMBAYARAN). Status terakhir diambil lewat
        // GET /v2/{order_id}/status lalu disimpan dengan alur yang
        // sama seperti notifikasi webhook.
        // =========================================================
        private async Task SyncPaymentStatusAsync(
            MySql.Data.MySqlClient.MySqlConnection conn,
            Payment payment)
        {
            if (payment.IdStatusPayment
                    != PaymentStatusMap.MenungguPembayaran
                || string.IsNullOrWhiteSpace(payment.MidtransOrderId))
            {
                return;
            }

            MidtransNotification? status;

            try
            {
                status = await midtrans.GetTransactionStatusAsync(
                    payment.MidtransOrderId);
            }
            catch (MidtransException e)
            {
                // Midtrans tidak dapat dihubungi: polling tetap
                // mengembalikan status yang tersimpan di database.
                logger.LogWarning(
                    "Sinkronisasi status order {OrderId} gagal: {Message}",
                    payment.MidtransOrderId,
                    e.Message);

                return;
            }

            if (status == null
                || string.IsNullOrWhiteSpace(status.TransactionStatus))
            {
                return;
            }

            var mappedStatus = MapPaymentStatus(
                status.TransactionStatus,
                status.FraudStatus);

            if (payment.IdStatusPayment
                == PaymentStatusMap.ToId(mappedStatus))
            {
                return;
            }

            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();

            await ApplyPaymentStatusAsync(
                conn,
                transaction,
                payment,
                mappedStatus,
                status);

            transaction.Commit();

            payment.IdStatusPayment = PaymentStatusMap.ToId(mappedStatus);

            logger.LogInformation(
                "Status Pesanan {IdPesanan} disinkronkan dari Midtrans menjadi {Status}",
                payment.IdPesanan,
                mappedStatus);
        }

        // =========================================================
        // HANDLE MIDTRANS NOTIFICATION
        //
        // Idempotent: notifikasi dengan status yang sama
        // tidak akan mengubah database dua kali.
        // =========================================================
        public async Task<NotificationResult> HandleNotificationAsync(
            MidtransNotification notification)
        {
            // 1. Validasi signature
            if (!midtrans.ValidateSignature(
                notification.OrderId,
                notification.StatusCode,
                notification.GrossAmount,
                notification.SignatureKey))
            {
                logger.LogWarning(
                    "Notifikasi Midtrans dengan signature tidak valid ditolak.");

                return NotificationResult.Invalid(
                    "Signature notifikasi tidak valid");
            }

            var mappedStatus = MapPaymentStatus(
                notification.TransactionStatus,
                notification.FraudStatus);

            using var conn = db.connect();

            const string paymentQuery = @"
                SELECT
                    id AS Id,
                    idPesanan AS IdPesanan,
                    idStatusPayment AS IdStatusPayment,
                    midtrans_order_id AS MidtransOrderId,
                    midtrans_transaction_id AS MidtransTransactionId,
                    snap_token AS SnapToken,
                    payment_type AS PaymentType,
                    gross_amount AS GrossAmount,
                    transaction_status AS TransactionStatus,
                    transaction_time AS TransactionTime,
                    settlement_time AS SettlementTime,
                    expiry_time AS ExpiryTime
                FROM payments
                WHERE midtrans_order_id = @MidtransOrderId
                LIMIT 1;";

            var payment =
                await conn.QueryFirstOrDefaultAsync<Payment>(
                    paymentQuery,
                    new { MidtransOrderId = notification.OrderId });

            // Pesanan tidak dikenal: kembalikan OK agar tidak retry selamanya
            if (payment == null)
            {
                logger.LogWarning(
                    "Notifikasi Midtrans untuk order {OrderId} tidak ditemukan.",
                    notification.OrderId);

                return NotificationResult.Ok(
                    "Pesanan tidak ditemukan, notifikasi diabaikan");
            }

            // 2. Validasi jumlah pembayaran
            if (!decimal.TryParse(
                    notification.GrossAmount,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var grossAmount)
                || grossAmount != payment.GrossAmount)
            {
                logger.LogWarning(
                    "Jumlah pembayaran tidak sesuai untuk order {OrderId}.",
                    notification.OrderId);

                return NotificationResult.Ok(
                    "Jumlah pembayaran tidak sesuai");
            }

            // 3. Idempotency: status sama tidak diproses ulang
            if (payment.IdStatusPayment
                == PaymentStatusMap.ToId(mappedStatus))
            {
                return NotificationResult.Ok(
                    "Notifikasi sudah diproses");
            }

            // 4. Update payment dalam satu transaksi
            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();

            await ApplyPaymentStatusAsync(
                conn,
                transaction,
                payment,
                mappedStatus,
                notification);

            transaction.Commit();

            logger.LogInformation(
                "Pembayaran Pesanan {IdPesanan} diperbarui menjadi {Status}",
                payment.IdPesanan,
                mappedStatus);

            return NotificationResult.Ok(
                "Notifikasi berhasil diproses");
        }

        // =========================================================
        // CANCEL PAYMENT
        //
        // idUser didapat dari Bearer Token.
        //
        // Transaksi yang masih MENUNGGU PEMBAYARAN dibatalkan.
        // Bila transaksi sudah terbuat di Midtrans, API Cancel
        // Midtrans dipanggil lebih dulu, lalu idStatusPayment dan
        // status Pesanan diperbarui mengikuti status akhirnya.
        // =========================================================
        public async Task<PaymentResult> CancelPaymentAsync(
            int idPesanan,
            int idUser)
        {
            using var conn = db.connect();

            const string paymentQuery = @"
                SELECT
                    p.id AS Id,
                    p.idPesanan AS IdPesanan,
                    p.idStatusPayment AS IdStatusPayment,
                    p.midtrans_order_id AS MidtransOrderId,
                    p.snap_token AS SnapToken,
                    p.gross_amount AS GrossAmount
                FROM payments p
                INNER JOIN Pesanan ps
                    ON ps.id = p.idPesanan
                WHERE
                    p.idPesanan = @IdPesanan
                    AND ps.idUser = @IdUser
                ORDER BY p.id DESC
                LIMIT 1;";

            var payment =
                await conn.QueryFirstOrDefaultAsync<Payment>(
                    paymentQuery,
                    new
                    {
                        IdPesanan = idPesanan,
                        IdUser = idUser
                    });

            if (payment == null)
            {
                return PaymentResult.Fail(
                    "Pembayaran tidak ditemukan");
            }

            // Pembayaran yang sudah lunas tidak dapat dibatalkan
            if (payment.IdStatusPayment == PaymentStatusMap.Dibayar)
            {
                return PaymentResult.Fail(
                    "Pembayaran sudah dibayar");
            }

            // Idempotent: pembayaran yang sudah dibatalkan
            // tidak diproses ulang.
            if (payment.IdStatusPayment == PaymentStatusMap.Dibatalkan)
            {
                return PaymentResult.Ok(
                    BuildPaymentResponse(payment),
                    "Pembayaran sudah dibatalkan");
            }

            string? transactionStatus = null;

            // Transaksi Midtrans hanya bisa dibatalkan bila sudah
            // terbuat di Midtrans (Snap token ada).
            if (!string.IsNullOrWhiteSpace(payment.SnapToken))
            {
                var cancel =
                    await midtrans.CancelTransactionAsync(
                        payment.MidtransOrderId);

                transactionStatus = cancel?.TransactionStatus;

                // API Cancel tidak selalu mengembalikan
                // transaction_status (mis. 412 karena transaksi
                // sudah settlement). Ambil status terakhir dari
                // Midtrans sebelum memutuskan.
                if (string.IsNullOrWhiteSpace(transactionStatus))
                {
                    var status =
                        await midtrans.GetTransactionStatusAsync(
                            payment.MidtransOrderId);

                    transactionStatus = status?.TransactionStatus;
                }
            }

            // Transaksi yang belum terbuat di Midtrans langsung
            // ditandai dibatalkan.
            var mappedStatus =
                string.IsNullOrWhiteSpace(transactionStatus)
                    ? PaymentStatusMap.Cancelled
                    : MapPaymentStatus(transactionStatus, null);

            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();

            const string updatePayment = @"
                UPDATE payments
                SET
                    idStatusPayment = @IdStatusPayment,
                    transaction_status = @TransactionStatus
                WHERE id = @Id;";

            await conn.ExecuteAsync(
                updatePayment,
                new
                {
                    Id = payment.Id,
                    IdStatusPayment =
                        PaymentStatusMap.ToId(mappedStatus),
                    TransactionStatus = transactionStatus
                },
                transaction);

            // Transaksi yang batal / kedaluwarsa membuat
            // pesanan ikut dibatalkan.
            if (PaymentStatusMap.IsCancelled(mappedStatus))
            {
                await MarkPesananDibatalkanAsync(
                    conn,
                    transaction,
                    payment.IdPesanan);
            }

            transaction.Commit();

            logger.LogInformation(
                "Pembayaran Pesanan {IdPesanan} diperbarui menjadi {Status}",
                payment.IdPesanan,
                mappedStatus);

            // Midtrans menolak cancel karena transaksi sudah lunas
            if (mappedStatus == PaymentStatusMap.Paid)
            {
                return PaymentResult.Fail(
                    "Pembayaran sudah dibayar");
            }

            payment.IdStatusPayment = PaymentStatusMap.ToId(mappedStatus);

            return PaymentResult.Ok(
                BuildPaymentResponse(payment),
                "Pembayaran berhasil dibatalkan");
        }

        // =========================================================
        // MAP MIDTRANS STATUS KE STATUS INTERNAL
        // =========================================================
        private static string MapPaymentStatus(
            string? transactionStatus,
            string? fraudStatus)
        {
            return transactionStatus switch
            {
                "capture" =>
                    fraudStatus == "challenge"
                        ? PaymentStatusMap.Pending
                        : PaymentStatusMap.Paid,
                "settlement" => PaymentStatusMap.Paid,
                "pending" => PaymentStatusMap.Pending,
                "deny" => PaymentStatusMap.Failed,
                "cancel" => PaymentStatusMap.Cancelled,
                "expire" => PaymentStatusMap.Expired,
                "refund" => PaymentStatusMap.Refunded,
                "partial_refund" => PaymentStatusMap.Refunded,
                "chargeback" => PaymentStatusMap.Failed,
                "partial_chargeback" => PaymentStatusMap.Failed,
                _ => PaymentStatusMap.Pending
            };
        }

        // =========================================================
        // SIMPAN STATUS PEMBAYARAN
        //
        // Dipakai notifikasi webhook & sinkronisasi status.
        // Status batal / kedaluwarsa sekaligus menandai Pesanan
        // sebagai dibatalkan dalam transaksi yang sama.
        // =========================================================
        private static async Task ApplyPaymentStatusAsync(
            MySql.Data.MySqlClient.MySqlConnection conn,
            MySql.Data.MySqlClient.MySqlTransaction transaction,
            Payment payment,
            string mappedStatus,
            MidtransNotification source)
        {
            const string updatePayment = @"
                UPDATE payments
                SET
                    idStatusPayment = @IdStatusPayment,
                    midtrans_transaction_id = @MidtransTransactionId,
                    payment_type = @PaymentType,
                    transaction_status = @TransactionStatus,
                    transaction_time = @TransactionTime,
                    settlement_time = @SettlementTime,
                    expiry_time = @ExpiryTime
                WHERE id = @Id;";

            await conn.ExecuteAsync(
                updatePayment,
                new
                {
                    Id = payment.Id,
                    IdStatusPayment =
                        PaymentStatusMap.ToId(mappedStatus),
                    MidtransTransactionId = source.TransactionId,
                    PaymentType = source.PaymentType,
                    TransactionStatus = source.TransactionStatus,
                    TransactionTime = source.TransactionTime,
                    SettlementTime = source.SettlementTime,
                    ExpiryTime = source.ExpiryTime
                },
                transaction);

            if (PaymentStatusMap.IsCancelled(mappedStatus))
            {
                await MarkPesananDibatalkanAsync(
                    conn,
                    transaction,
                    payment.IdPesanan);
            }
        }

        // =========================================================
        // RESPONSE PAYMENT DARI MODEL DATABASE
        // =========================================================
        private static PaymentResponse BuildPaymentResponse(
            Payment payment)
        {
            return new PaymentResponse
            {
                IdPesanan = payment.IdPesanan,
                MidtransOrderId = payment.MidtransOrderId,
                GrossAmount = payment.GrossAmount,
                SnapToken = payment.SnapToken,
                PaymentStatus = PaymentStatusMap.ToCode(
                    payment.IdStatusPayment)
            };
        }

        // =========================================================
        // TANDAI PESANAN SEBAGAI DIBATALKAN
        //
        // idStatusPengerjaan dicari lewat nama agar tidak
        // bergantung pada id hasil seed.
        // =========================================================
        private static async Task MarkPesananDibatalkanAsync(
            MySql.Data.MySqlClient.MySqlConnection conn,
            MySql.Data.MySqlClient.MySqlTransaction transaction,
            int idPesanan)
        {
            var idStatus = await conn.QueryFirstOrDefaultAsync<int?>(
                @"SELECT id
                  FROM status_pengerjaan
                  WHERE nama = @Nama
                  LIMIT 1;",
                new { Nama = PesananDibatalkan },
                transaction);

            if (idStatus == null)
            {
                return;
            }

            await conn.ExecuteAsync(
                @"UPDATE Pesanan
                  SET idStatusPengerjaan = @IdStatusPengerjaan
                  WHERE id = @IdPesanan;",
                new
                {
                    IdPesanan = idPesanan,
                    IdStatusPengerjaan = idStatus.Value
                },
                transaction);
        }
    }
}
