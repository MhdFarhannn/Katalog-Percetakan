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
                    u.Nama AS NamaUser,
                    u.Email AS Email
                FROM Pesanan ps
                INNER JOIN User u
                    ON u.Id = ps.idUser
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

            // Idempotent: kembalikan Snap token yang masih pending
            const string existingQuery = @"
                SELECT
                    id AS Id,
                    idPesanan AS IdPesanan,
                    midtrans_order_id AS MidtransOrderId,
                    midtrans_transaction_id AS MidtransTransactionId,
                    snap_token AS SnapToken,
                    payment_type AS PaymentType,
                    gross_amount AS GrossAmount,
                    transaction_status AS TransactionStatus,
                    payment_status AS PaymentStatus,
                    transaction_time AS TransactionTime,
                    settlement_time AS SettlementTime,
                    expiry_time AS ExpiryTime
                FROM payments
                WHERE
                    idPesanan = @IdPesanan
                    AND payment_status = 'pending'
                    AND snap_token IS NOT NULL
                ORDER BY id DESC
                LIMIT 1;";

            var existing =
                await conn.QueryFirstOrDefaultAsync<Payment>(
                    existingQuery,
                    new { IdPesanan = idPesanan });

            if (existing != null)
            {
                return PaymentResult.Ok(
                    new PaymentResponse
                    {
                        IdPesanan = existing.IdPesanan,
                        MidtransOrderId = existing.MidtransOrderId,
                        GrossAmount = existing.GrossAmount,
                        SnapToken = existing.SnapToken,
                        PaymentStatus = existing.PaymentStatus
                    },
                    "Pembayaran sudah dibuat");
            }

            var paidCount = await conn.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*) FROM payments
                  WHERE idPesanan = @IdPesanan
                      AND payment_status = 'paid';",
                new { IdPesanan = idPesanan });

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
                    midtrans_order_id,
                    snap_token,
                    gross_amount,
                    payment_status
                )
                VALUES
                (
                    @IdPesanan,
                    @MidtransOrderId,
                    @SnapToken,
                    @GrossAmount,
                    'pending'
                );

                SELECT LAST_INSERT_ID();";

            await conn.ExecuteScalarAsync<int>(
                insertQuery,
                new
                {
                    IdPesanan = pesanan.Id,
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
                PaymentStatus = "pending"
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
                    p.idPesanan AS IdPesanan,
                    p.midtrans_order_id AS MidtransOrderId,
                    p.gross_amount AS GrossAmount,
                    p.snap_token AS SnapToken,
                    p.payment_status AS PaymentStatus
                FROM payments p
                INNER JOIN Pesanan ps
                    ON ps.id = p.idPesanan
                WHERE
                    p.idPesanan = @IdPesanan
                    AND ps.idUser = @IdUser
                ORDER BY p.id DESC
                LIMIT 1;";

            return await conn.QueryFirstOrDefaultAsync<PaymentResponse>(
                query,
                new
                {
                    IdPesanan = idPesanan,
                    IdUser = idUser
                });
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
                    midtrans_order_id AS MidtransOrderId,
                    midtrans_transaction_id AS MidtransTransactionId,
                    snap_token AS SnapToken,
                    payment_type AS PaymentType,
                    gross_amount AS GrossAmount,
                    transaction_status AS TransactionStatus,
                    payment_status AS PaymentStatus,
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
            if (string.Equals(
                payment.PaymentStatus,
                mappedStatus,
                StringComparison.OrdinalIgnoreCase))
            {
                return NotificationResult.Ok(
                    "Notifikasi sudah diproses");
            }

            // 4. Update payment dalam satu transaksi
            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();

            const string updatePayment = @"
                UPDATE payments
                SET
                    midtrans_transaction_id = @MidtransTransactionId,
                    payment_type = @PaymentType,
                    transaction_status = @TransactionStatus,
                    payment_status = @PaymentStatus,
                    transaction_time = @TransactionTime,
                    settlement_time = @SettlementTime,
                    expiry_time = @ExpiryTime
                WHERE id = @Id;";

            await conn.ExecuteAsync(
                updatePayment,
                new
                {
                    Id = payment.Id,
                    MidtransTransactionId =
                        notification.TransactionId,
                    PaymentType = notification.PaymentType,
                    TransactionStatus =
                        notification.TransactionStatus,
                    PaymentStatus = mappedStatus,
                    TransactionTime =
                        notification.TransactionTime,
                    SettlementTime =
                        notification.SettlementTime,
                    ExpiryTime = notification.ExpiryTime
                },
                transaction);

            transaction.Commit();

            logger.LogInformation(
                "Pembayaran Pesanan {IdPesanan} diperbarui menjadi {Status}",
                payment.IdPesanan,
                mappedStatus);

            return NotificationResult.Ok(
                "Notifikasi berhasil diproses");
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
                        ? "pending"
                        : "paid",
                "settlement" => "paid",
                "pending" => "pending",
                "deny" => "failed",
                "cancel" => "cancelled",
                "expire" => "expired",
                "refund" => "refunded",
                "partial_refund" => "refunded",
                "chargeback" => "failed",
                "partial_chargeback" => "failed",
                _ => "pending"
            };
        }
    }
}
