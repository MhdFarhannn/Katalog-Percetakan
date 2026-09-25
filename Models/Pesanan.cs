using System.Text.Json.Serialization;

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

        public DateTime? UpdatedAt { get; set; }

        public DateTime? DeletedAt { get; set; }
    }

    public class PesananRequest
    {
        public int IdAlamat { get; set; }

        public List<PesananDetailRequest> Items { get; set; } = new();
    }

    // =========================================================
    // PUT /api/v1/pesanan/{id}/status (Admin)
    //
    // Status boleh dikirim sebagai id (IdStatusPengerjaan)
    // ATAU nama (StatusPengerjaan). Minimal salah satu terisi.
    // =========================================================
    public class PesananStatusRequest
    {
        public int? IdStatusPengerjaan { get; set; }

        public string? StatusPengerjaan { get; set; }
    }

    // =========================================================
    // GET /api/v1/pesanan/history (Order History)
    //
    // Parameter diikat dari query string. Tanggal memakai
    // format yyyy-MM-dd. Page / PageSize di-clamp di service.
    // =========================================================
    public class PesananHistoryQuery
    {
        public string? StartDate { get; set; }

        public string? EndDate { get; set; }

        public int? IdStatusPengerjaan { get; set; }

        // Hanya dipakai Admin/Petugas; Pelanggan selalu
        // memakai idUser miliknya sendiri (dari JWT).
        public int? IdUser { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }

    public class PesananHistoryResponse
    {
        public List<PesananResponse> Items { get; set; } = new();

        public int Total { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }
    }

    public class PesananDetailRequest
    {
        public int IdProduct { get; set; }

        public int? IdUkuranProduk { get; set; }

        public string? UkuranCustom { get; set; }

        public int Qty { get; set; }

        // Dimensi custom (hanya dipakai product PerArea / PerLength).
        // Satuannya mengikuti product.dimension_unit (cm / meter).
        // TIDAK ada field harga di sini: harga dihitung server.
        public decimal? Width { get; set; }

        public decimal? Height { get; set; }

        public decimal? Length { get; set; }

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

        public string PaymentStatus { get; set; } = PaymentStatusMap.Unpaid;

        // idStatusPayment pembayaran terakhir (JOIN status_payment).
        // Tidak dikirim ke API, hanya dipakai untuk memetakan
        // PaymentStatus (kode) di PesananServices.
        [JsonIgnore]
        public int? IdStatusPayment { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

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

        // Rate snapshot: per unit / per m2 / per meter (sesuai mode).
        public decimal HargaSatuan { get; set; }

        // Snapshot pricing supaya harga lama tidak berubah walau
        // konfigurasi product diubah kemudian.
        public string? PricingMode { get; set; }

        public decimal? WidthMeters { get; set; }

        public decimal? HeightMeters { get; set; }

        public decimal? LengthMeters { get; set; }

        public string? DimensionUnit { get; set; }

        // Dihitung saat query (width_m x height_m), bukan disimpan.
        public decimal? AreaM2 { get; set; }

        // COALESCE(subtotal, harga_satuan x qty) untuk baris lama.
        public decimal Subtotal { get; set; }

        public string? Notes { get; set; }

        public string? DesainFilePath { get; set; }

        public string? DesainText { get; set; }
    }
}