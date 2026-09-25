namespace Katalog.Models
{
    // =========================================================
    // PRICING MODE
    //
    // Menentukan bagaimana harga sebuah product dihitung.
    // NILAI INI DISIMPAN PER PRODUCT (kolom product.pricing_mode)
    // dan TIDAK BOLEH ditentukan dari nama product.
    //
    //   Fixed     : harga tetap per unit (mis. Standing Banner 60x160).
    //   PerArea   : harga per meter persegi  (luas = lebar x tinggi).
    //   PerLength : harga per meter lurus.
    //   PerUnit   : harga per unit / kuantitas.
    //   Custom    : cadangan untuk aturan khusus (diperlakukan seperti
    //               Fixed sampai strategi khusus ditambahkan).
    // =========================================================
    public enum PricingMode
    {
        Fixed,
        PerArea,
        PerLength,
        PerUnit,
        Custom
    }

    // =========================================================
    // DIMENSION UNIT
    //
    // Satuan internal yang dipakai untuk menghitung dimensi.
    // Nilai ini menentukan cara mengonversi angka yang dikirim
    // customer (lebar / tinggi / panjang) menjadi METER, yang
    // merupakan satuan internal baku pada perhitungan harga.
    // =========================================================
    public enum DimensionUnit
    {
        Centimeter,
        Meter
    }

    // =========================================================
    // KONVERSI & PARSING SATUAN / MODE
    //
    // Semua konversi satuan terpusat di sini (satu boundary),
    // bukan disebar di service / controller.
    // =========================================================
    public static class PricingUnits
    {
        public static decimal ToMeters(decimal value, DimensionUnit unit)
        {
            return unit == DimensionUnit.Centimeter
                ? value / 100m
                : value;
        }

        // Nilai kanonik yang disimpan ke database.
        public static string ToCode(DimensionUnit unit)
        {
            return unit == DimensionUnit.Centimeter
                ? "centimeter"
                : "meter";
        }

        public static string ToCode(PricingMode mode)
        {
            return mode.ToString();
        }

        public static PricingMode ParseMode(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                // Product lama (sebelum migrasi) tetap berperilaku
                // seperti sebelumnya: harga tetap per unit.
                return PricingMode.Fixed;
            }

            return Enum.TryParse<PricingMode>(
                value.Trim(),
                ignoreCase: true,
                out var mode)
                ? mode
                : PricingMode.Fixed;
        }

        public static DimensionUnit ParseUnit(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DimensionUnit.Meter;
            }

            return value.Trim().ToLowerInvariant() switch
            {
                "cm" or "centimeter" or "centimeters" =>
                    DimensionUnit.Centimeter,
                "m" or "meter" or "meters" or "metre" or "metres" =>
                    DimensionUnit.Meter,
                _ => DimensionUnit.Meter
            };
        }

        public static bool IsValidMode(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            return Enum.TryParse<PricingMode>(
                value.Trim(),
                ignoreCase: true,
                out _);
        }

        public static bool IsValidUnit(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            return value.Trim().ToLowerInvariant() switch
            {
                "cm" or "centimeter" or "centimeters" => true,
                "m" or "meter" or "meters" or "metre" or "metres" => true,
                _ => false
            };
        }
    }

    // =========================================================
    // KONFIGURASI HARGA
    //
    // Dibentuk dari product (+ Ukuran_Produk bila varian dipilih).
    // Penting: harga akhir SELALU berasal dari konfigurasi ini,
    // bukan dari nilai yang dikirim client.
    // =========================================================
    public class PricingConfiguration
    {
        public PricingMode Mode { get; set; } = PricingMode.Fixed;

        // product.harga — arti tergantung Mode:
        //   Fixed     = harga satuan dasar
        //   PerArea   = harga per m2
        //   PerLength = harga per meter
        //   PerUnit   = harga per unit
        public decimal BasePrice { get; set; }

        // Ukuran_Produk.harga_tambahan — tambahan harga lama.
        public decimal AdditionalPrice { get; set; }

        // Ukuran_Produk.harga — harga absolut varian (override).
        // NULL berarti pakai BasePrice + AdditionalPrice.
        public decimal? VariantPrice { get; set; }

        // Ukuran_Produk.panjang_cm / lebar_cm — dimensi tetap (cm).
        public int? PanjangCm { get; set; }
        public int? LebarCm { get; set; }

        // Satuan input dimensi dari customer.
        public DimensionUnit Unit { get; set; } = DimensionUnit.Meter;

        // Harga satuan efektif (rate) sebelum faktor dimensi & qty.
        public decimal EffectiveRate =>
            VariantPrice ?? (BasePrice + AdditionalPrice);
    }

    // =========================================================
    // INPUT HARGA
    //
    // Hanya data yang dibutuhkan untuk menghitung. TIDAK ADA
    // field harga di sini — client tidak boleh menentukan harga.
    // =========================================================
    public class PricingInput
    {
        public int IdProduct { get; set; }

        public int? IdUkuranProduk { get; set; }

        public int Quantity { get; set; }

        // Dimensi dalam satuan konfigurasi product (cm / meter).
        public decimal? Width { get; set; }

        public decimal? Height { get; set; }

        public decimal? Length { get; set; }
    }

    // =========================================================
    // HASIL HARGA
    //
    // Snapshot yang dipakai untuk mengisi Pesanan_Detail dan
    // Pesanan.total_harga.
    // =========================================================
    public class PricingResult
    {
        public bool Success { get; set; }

        public string? Error { get; set; }

        public PricingMode Mode { get; set; }

        // Rate per unit / per m2 / per meter (snapshot harga_satuan).
        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        public decimal? WidthMeters { get; set; }

        public decimal? HeightMeters { get; set; }

        public decimal? LengthMeters { get; set; }

        public DimensionUnit Unit { get; set; } = DimensionUnit.Meter;

        public decimal AreaM2 { get; set; }

        public decimal Subtotal { get; set; }
    }

    // =========================================================
    // BARIS BACA DATABASE (khusus PricingServices)
    // =========================================================
    public class ProductPricingRow
    {
        public decimal Harga { get; set; }

        public string? PricingMode { get; set; }

        public string? DimensionUnit { get; set; }
    }

    public class UkuranPricingRow
    {
        public decimal HargaTambahan { get; set; }

        public decimal? Harga { get; set; }

        public int? PanjangCm { get; set; }

        public int? LebarCm { get; set; }
    }

    // =========================================================
    // VARIAN PRODUK (Ukuran_Produk) — untuk endpoint katalog.
    // =========================================================
    public class UkuranProduk
    {
        public int Id { get; set; }

        public int IdProduct { get; set; }

        public string Nama { get; set; } = string.Empty;

        public decimal HargaTambahan { get; set; }

        public decimal? Harga { get; set; }

        public int? PanjangCm { get; set; }

        public int? LebarCm { get; set; }
    }
}
