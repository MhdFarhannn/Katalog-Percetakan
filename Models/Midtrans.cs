using System.Text.Json.Serialization;

namespace Katalog.Models
{
    public class MidtransSnapRequest
    {
        [JsonPropertyName("transaction_details")]
        public MidtransTransactionDetails TransactionDetails { get; set; } = new();

        [JsonPropertyName("item_details")]
        public List<MidtransItemDetails>? ItemDetails { get; set; }

        [JsonPropertyName("customer_details")]
        public MidtransCustomerDetails? CustomerDetails { get; set; }
    }

    public class MidtransTransactionDetails
    {
        [JsonPropertyName("order_id")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("gross_amount")]
        public decimal GrossAmount { get; set; }
    }

    public class MidtransItemDetails
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class MidtransCustomerDetails
    {
        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }

    public class MidtransSnapResponse
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("redirect_url")]
        public string? RedirectUrl { get; set; }
    }

    public class MidtransNotification
    {
        [JsonPropertyName("transaction_id")]
        public string? TransactionId { get; set; }

        [JsonPropertyName("order_id")]
        public string? OrderId { get; set; }

        [JsonPropertyName("gross_amount")]
        public string? GrossAmount { get; set; }

        [JsonPropertyName("payment_type")]
        public string? PaymentType { get; set; }

        [JsonPropertyName("transaction_status")]
        public string? TransactionStatus { get; set; }

        [JsonPropertyName("fraud_status")]
        public string? FraudStatus { get; set; }

        [JsonPropertyName("status_code")]
        public string? StatusCode { get; set; }

        [JsonPropertyName("signature_key")]
        public string? SignatureKey { get; set; }

        [JsonPropertyName("transaction_time")]
        public string? TransactionTime { get; set; }

        [JsonPropertyName("settlement_time")]
        public string? SettlementTime { get; set; }

        [JsonPropertyName("expiry_time")]
        public string? ExpiryTime { get; set; }
    }
}
