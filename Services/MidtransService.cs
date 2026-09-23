using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Katalog.Models;

namespace Katalog.Services
{
    public class MidtransException : Exception
    {
        public MidtransException(string message) : base(message)
        {
        }
    }

    public class MidtransService
    {
        private const string SandboxSnapUrl =
            "https://app.sandbox.midtrans.com/snap/v1/transactions";

        private const string ProductionSnapUrl =
            "https://app.midtrans.com/snap/v1/transactions";

        private const string SandboxApiUrl =
            "https://api.sandbox.midtrans.com/v2";

        private const string ProductionApiUrl =
            "https://api.midtrans.com/v2";

        private readonly HttpClient _http;
        private readonly ILogger<MidtransService> _logger;
        private readonly string _serverKey;
        private readonly bool _isProduction;

        public MidtransService(
            HttpClient http,
            ILogger<MidtransService> logger)
        {
            _http = http;
            _logger = logger;
            _serverKey = Env.Value["Midtrans:ServerKey"] ?? string.Empty;
            _isProduction =
                bool.TryParse(
                    Env.Value["Midtrans:IsProduction"],
                    out var isProduction)
                && isProduction;
        }

        public bool IsProduction => _isProduction;

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_serverKey);

        private string SnapUrl =>
            _isProduction
                ? ProductionSnapUrl
                : SandboxSnapUrl;

        private string ApiUrl =>
            _isProduction
                ? ProductionApiUrl
                : SandboxApiUrl;

        private AuthenticationHeaderValue BasicAuth()
        {
            // Midtrans Basic Auth: username = ServerKey, password = empty
            var raw = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_serverKey}:"));

            return new AuthenticationHeaderValue("Basic", raw);
        }

        // =========================================================
        // CREATE SNAP TRANSACTION
        // =========================================================
        public async Task<MidtransSnapResponse> CreateSnapTransactionAsync(
            MidtransSnapRequest request)
        {
            if (!IsConfigured)
            {
                throw new MidtransException(
                    "Midtrans ServerKey belum dikonfigurasi.");
            }

            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                SnapUrl)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json")
            };

            httpRequest.Headers.Authorization = BasicAuth();
            httpRequest.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                using var response = await _http.SendAsync(httpRequest);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Midtrans Snap gagal. Status: {Status}",
                        (int)response.StatusCode);

                    throw new MidtransException(
                        $"Midtrans Snap error ({(int)response.StatusCode}).");
                }

                var result =
                    JsonSerializer.Deserialize<MidtransSnapResponse>(body);

                if (result == null
                    || string.IsNullOrWhiteSpace(result.Token))
                {
                    throw new MidtransException(
                        "Midtrans tidak mengembalikan Snap token.");
                }

                return result;
            }
            catch (TaskCanceledException)
            {
                throw new MidtransException("Midtrans timeout.");
            }
            catch (HttpRequestException)
            {
                throw new MidtransException(
                    "Tidak dapat terhubung ke Midtrans.");
            }
        }

        // =========================================================
        // CANCEL TRANSACTION
        //
        // POST /v2/{order_id}/cancel
        //
        // Status non-2xx tetap dikembalikan apa adanya karena
        // Midtrans mengirim status_message (mis. transaksi sudah
        // settlement). null berarti body tidak bisa dibaca.
        // =========================================================
        public async Task<MidtransCancelResponse?> CancelTransactionAsync(
            string orderId)
        {
            if (!IsConfigured)
            {
                throw new MidtransException(
                    "Midtrans ServerKey belum dikonfigurasi.");
            }

            var url =
                $"{ApiUrl}/{Uri.EscapeDataString(orderId)}/cancel";

            using var httpRequest =
                new HttpRequestMessage(HttpMethod.Post, url);

            httpRequest.Headers.Authorization = BasicAuth();
            httpRequest.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                using var response = await _http.SendAsync(httpRequest);
                var body = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(body))
                {
                    _logger.LogError(
                        "Midtrans cancel gagal. Status: {Status}",
                        (int)response.StatusCode);

                    return null;
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Midtrans cancel order {OrderId} status {Status}.",
                        orderId,
                        (int)response.StatusCode);
                }

                return JsonSerializer.Deserialize<MidtransCancelResponse>(
                    body);
            }
            catch (TaskCanceledException)
            {
                throw new MidtransException("Midtrans timeout.");
            }
            catch (HttpRequestException)
            {
                throw new MidtransException(
                    "Tidak dapat terhubung ke Midtrans.");
            }
        }

        // =========================================================
        // QUERY TRANSACTION STATUS
        // =========================================================
        public async Task<MidtransNotification?> GetTransactionStatusAsync(
            string orderId)
        {
            if (!IsConfigured)
            {
                throw new MidtransException(
                    "Midtrans ServerKey belum dikonfigurasi.");
            }

            var url =
                $"{ApiUrl}/{Uri.EscapeDataString(orderId)}/status";

            using var httpRequest =
                new HttpRequestMessage(HttpMethod.Get, url);

            httpRequest.Headers.Authorization = BasicAuth();
            httpRequest.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                using var response = await _http.SendAsync(httpRequest);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Midtrans status gagal. Status: {Status}",
                        (int)response.StatusCode);

                    return null;
                }

                var body = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<MidtransNotification>(body);
            }
            catch (TaskCanceledException)
            {
                throw new MidtransException("Midtrans timeout.");
            }
            catch (HttpRequestException)
            {
                throw new MidtransException(
                    "Tidak dapat terhubung ke Midtrans.");
            }
        }

        // =========================================================
        // NOTIFICATION SIGNATURE VALIDATION
        //
        // signature_key = SHA512(order_id + status_code
        //                        + gross_amount + ServerKey)
        // =========================================================
        public bool ValidateSignature(
            string? orderId,
            string? statusCode,
            string? grossAmount,
            string? signatureKey)
        {
            if (string.IsNullOrEmpty(orderId)
                || string.IsNullOrEmpty(statusCode)
                || string.IsNullOrEmpty(grossAmount)
                || string.IsNullOrEmpty(signatureKey)
                || !IsConfigured)
            {
                return false;
            }

            var raw =
                orderId + statusCode + grossAmount + _serverKey;

            var hash = SHA512.HashData(
                Encoding.UTF8.GetBytes(raw));

            var expected = Convert.ToHexString(hash)
                .ToLowerInvariant();

            var actual = signatureKey.ToLowerInvariant();

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(actual));
        }
    }
}
