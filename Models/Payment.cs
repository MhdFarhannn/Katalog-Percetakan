namespace Katalog.Models
{
    public class Payment
    {
        public int Id { get; set; }

        public int IdPesanan { get; set; }

        public string MidtransOrderId { get; set; } = string.Empty;

        public string? MidtransTransactionId { get; set; }

        public string? SnapToken { get; set; }

        public string? PaymentType { get; set; }

        public decimal GrossAmount { get; set; }

        public string? TransactionStatus { get; set; }

        public string PaymentStatus { get; set; } = "pending";

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
