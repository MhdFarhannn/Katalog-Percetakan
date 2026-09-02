namespace Katalog.Models
{
    public class Alamat
    {
        public int Id { get; set; }

        public int IdUser { get; set; }

        public string Content { get; set; } = string.Empty;

        public string NoTelepon { get; set; } = string.Empty;
    }

    public class AlamatRequest
    {
        public string? Content { get; set; }
        public string? NoTelepon { get; set; }
    }

    public class AlamatResponse
    {
        public int Id { get; set; }
        public int IdUser { get; set; }
        public string NamaUser { get; set; } = string.Empty;
        public string NoTelepon { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

}
