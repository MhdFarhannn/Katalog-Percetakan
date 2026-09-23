namespace Katalog.Models
{
    public class Payment
    {
        public int Id { get; set; }

        public int IdPesanan { get; set; }

        // FK ke status_payment (lihat PaymentStatusMap)
        public int IdStatusPayment { get; set; }

        public string MidtransOrderId { get; set; } = string.Empty;

        public string? MidtransTransactionId { get; set; }

        public string? SnapToken { get; set; }

        public string? PaymentType { get; set; }

        public decimal GrossAmount { get; set; }

        public string? TransactionStatus { get; set; }

        public string? TransactionTime { get; set; }

        public string? SettlementTime { get; set; }

        public string? ExpiryTime { get; set; }
    }

    public class PaymentResponse
    {
        public int IdPesanan { get; set; }

        public string MidtransOrderId { get; set; } = string.Empty;

        public decimal GrossAmount { get; set; }

        public string? SnapToken { get; set; }

        public string? RedirectUrl { get; set; }

        public string PaymentStatus { get; set; } = string.Empty;
    }

    public class PesananPaymentInfo
    {
        public int Id { get; set; }

        public int IdUser { get; set; }

        public string NamaUser { get; set; } = string.Empty;

        public string? Email { get; set; }

        public decimal TotalHarga { get; set; }

        public string StatusPengerjaan { get; set; } = string.Empty;
    }

    // =========================================================
    // STATUS PEMBAYARAN
    //
    // idStatusPayment pada tabel payments adalah FK ke
    // status_payment. Kode di bawah dipakai aplikasi & API,
    // sedangkan tabel status_payment menyimpan labelnya.
    // =========================================================
    public static class PaymentStatusMap
    {
        public const int MenungguPembayaran = 1;

        public const int Dibayar = 2;

        public const int Dibatalkan = 3;

        public const int Kedaluwarsa = 4;

        public const int Gagal = 5;

        public const int Dikembalikan = 6;

        public const string Pending = "pending";

        public const string Paid = "paid";

        public const string Cancelled = "cancelled";

        public const string Expired = "expired";

        public const string Failed = "failed";

        public const string Refunded = "refunded";

        public static string ToCode(int idStatusPayment)
        {
            return idStatusPayment switch
            {
                Dibayar => Paid,
                Dibatalkan => Cancelled,
                Kedaluwarsa => Expired,
                Gagal => Failed,
                Dikembalikan => Refunded,
                _ => Pending
            };
        }

        public static int ToId(string? paymentStatus)
        {
            return paymentStatus?.ToLowerInvariant() switch
            {
                Paid => Dibayar,
                Cancelled => Dibatalkan,
                Expired => Kedaluwarsa,
                Failed => Gagal,
                Refunded => Dikembalikan,
                _ => MenungguPembayaran
            };
        }

        public static bool IsCancelled(string? paymentStatus)
        {
            return string.Equals(
                    paymentStatus,
                    Cancelled,
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    paymentStatus,
                    Expired,
                    StringComparison.OrdinalIgnoreCase);
        }
    }

    public class PaymentResult
    {
        public bool Success { get; init; }

        public string Message { get; init; } = string.Empty;

        public PaymentResponse? Data { get; init; }

        public static PaymentResult Ok(
            PaymentResponse data,
            string message = "")
        {
            return new PaymentResult
            {
                Success = true,
                Data = data,
                Message = message
            };
        }

        public static PaymentResult Fail(string message)
        {
            return new PaymentResult
            {
                Success = false,
                Message = message
            };
        }
    }

    public class NotificationResult
    {
        public bool InvalidSignature { get; init; }

        public string Message { get; init; } = string.Empty;

        public static NotificationResult Ok(string message)
        {
            return new NotificationResult
            {
                InvalidSignature = false,
                Message = message
            };
        }

        public static NotificationResult Invalid(string message)
        {
            return new NotificationResult
            {
                InvalidSignature = true,
                Message = message
            };
        }
    }
}
