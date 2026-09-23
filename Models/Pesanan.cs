namespace Katalog.Models
{
    public class Pesanan
    {
        public int Id { get; set; }

        public int IdUser { get; set; }

        public int IdAlamat { get; set; }

        public int IdStatusPengerjaan { get; set; }

        public decimal TotalHarga { get; set; }

        public DateTime? CreatedAt { get; set; }
    }

    public class PesananRequest
    {
        public int IdAlamat { get; set; }

        public List<PesananDetailRequest> Items { get; set; } = new();
    }

    public class PesananDetailRequest
    {
        public int IdProduct { get; set; }

        public int? IdUkuranProduk { get; set; }

        public string? UkuranCustom { get; set; }

        public int Qty { get; set; }

        public string? Notes { get; set; }

        public string? DesainFilePath { get; set; }

        public string? DesainText { get; set; }

        // File desain dikirim sebagai multipart file field
        // (items[N].desain), BUKAN Base64 / JSON. Diisi dari form,
        // disimpan server, lalu dipetakan ke DesainFilePath.
        public IFormFile? Desain { get; set; }
    }

    public class PesananResponse
    {
        public int Id { get; set; }

        public int IdUser { get; set; }

        public string NamaUser { get; set; } = string.Empty;

        public int IdAlamat { get; set; }

        public string Alamat { get; set; } = string.Empty;

        public int IdStatusPengerjaan { get; set; }

        public string StatusPengerjaan { get; set; } = string.Empty;

        public decimal TotalHarga { get; set; }

        public string PaymentStatus { get; set; } = "unpaid";

        public DateTime? CreatedAt { get; set; }

        public List<PesananDetailResponse> Details { get; set; } = new();
    }

    public class PesananDetailResponse
    {
        public int Id { get; set; }

        public int IdPesanan { get; set; }

        public int IdProduct { get; set; }

        public string NamaProduct { get; set; } = string.Empty;

        public int? IdUkuranProduk { get; set; }

        public string? NamaUkuran { get; set; }

        public string? UkuranCustom { get; set; }

        public int Qty { get; set; }

        public decimal HargaSatuan { get; set; }

        public string? Notes { get; set; }

        public string? DesainFilePath { get; set; }

        public string? DesainText { get; set; }
    }
}