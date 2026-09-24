using Dapper;
using Katalog.Models;

namespace Katalog.Services
{
    public class LayananServices
    {
        private readonly Database db;
        private readonly IWebHostEnvironment environment;

        public LayananServices(
            Database db,
            IWebHostEnvironment environment)
        {
            this.db = db;
            this.environment = environment;
        }

        // ==========================================
        // CREATE
        // ==========================================

        public async Task<Layanan> CreateLayananAsync(
            Layanan layanan,
            IFormFile? image)
        {
            using var connection = db.connect();

            // Upload image jika ada
            if (image != null && image.Length > 0)
            {
                layanan.ImagePath = await SaveImageAsync(image);
            }

            const string insertQuery = @"
                INSERT INTO Layanan
                (
                    nama,
                    deskripsi,
                    imagePath,
                    background_color
                )
                VALUES
                (
                    @Nama,
                    @Deskripsi,
                    @ImagePath,
                    @BackgroundColor
                );";

            // Exec query insert terlebih dahulu
            await connection.ExecuteAsync(insertQuery, layanan);

            // Ambil ID terakhir yang di-insert dalam session koneksi yang sama
            var id = await connection.ExecuteScalarAsync<int>("SELECT LAST_INSERT_ID();");

            layanan.Id = id;

            return layanan;
        }


        // ==========================================
        // GET ALL
        // ==========================================

        public async Task<IEnumerable<Layanan>> GetLayananAsync()
        {
            using var connection = db.connect();

            const string query = @"
                SELECT
                    id AS Id,
                    nama AS Nama,
                    deskripsi AS Deskripsi,
                    imagePath AS ImagePath,
                    background_color AS BackgroundColor
                FROM Layanan
                ORDER BY id DESC";

            var result = await connection.QueryAsync<Layanan>(
                query
            );

            return result;
        }


        // ==========================================
        // GET BY ID
        // ==========================================

        public async Task<Layanan?> GetLayananByIdAsync(int id)
        {
            using var connection = db.connect();

            const string query = @"
                SELECT
                    id AS Id,
                    nama AS Nama,
                    deskripsi AS Deskripsi,
                    imagePath AS ImagePath,
                    background_color AS BackgroundColor
                FROM Layanan
                WHERE id = @Id";

            return await connection.QueryFirstOrDefaultAsync<Layanan>(
                query,
                new { Id = id }
            );
        }


        // ==========================================
        // PATCH / UPDATE
        // ==========================================

        public async Task<Layanan?> PatchLayananAsync(
            int id,
            Layanan layanan,
            IFormFile? image)
        {
            using var connection = db.connect();

            // Ambil data lama
            const string getQuery = @"
                SELECT
                    id AS Id,
                    nama AS Nama,
                    deskripsi AS Deskripsi,
                    imagePath AS ImagePath,
                    background_color AS BackgroundColor
                FROM Layanan
                WHERE id = @Id";

            var oldLayanan =
                await connection.QueryFirstOrDefaultAsync<Layanan>(
                    getQuery,
                    new { Id = id }
                );

            if (oldLayanan == null)
            {
                return null;
            }

            // Jika ada image baru
            if (image != null && image.Length > 0)
            {
                // Hapus image lama
                DeleteImage(oldLayanan.ImagePath);

                // Simpan image baru
                layanan.ImagePath = await SaveImageAsync(image);
            }
            else
            {
                // Jika tidak upload image, gunakan image lama
                layanan.ImagePath = oldLayanan.ImagePath;
            }

            const string updateQuery = @"
                UPDATE Layanan
                SET
                    nama = @Nama,
                    deskripsi = @Deskripsi,
                    imagePath = @ImagePath,
                    background_color = @BackgroundColor
                WHERE id = @Id";

            await connection.ExecuteAsync(
                updateQuery,
                new
                {
                    Id = id,
                    layanan.Nama,
                    layanan.Deskripsi,
                    layanan.ImagePath,
                    layanan.BackgroundColor
                }
            );

            layanan.Id = id;

            return layanan;
        }


        // ==========================================
        // DELETE
        // ==========================================

        public async Task<bool> DeleteLayananAsync(int id)
        {
            using var connection = db.connect();

            // Ambil data
            const string getQuery = @"
                SELECT imagePath AS ImagePath
                FROM Layanan
                WHERE id = @Id";

            var layanan =
                await connection.QueryFirstOrDefaultAsync<Layanan>(
                    getQuery,
                    new { Id = id }
                );

            if (layanan == null)
            {
                return false;
            }

            // Hapus database
            const string deleteQuery = @"
                DELETE FROM Layanan
                WHERE id = @Id";

            var result = await connection.ExecuteAsync(
                deleteQuery,
                new { Id = id }
            );

            // Hapus file
            if (result > 0)
            {
                DeleteImage(layanan.ImagePath);
            }

            return result > 0;
        }


        // ==========================================
        // SAVE IMAGE
        // ==========================================

        private async Task<string> SaveImageAsync(IFormFile image)
        {
            var folderPath = Path.Combine(
                environment.WebRootPath,
                "images",
                "layanan"
            );

            // Buat folder jika belum ada
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Ambil extension
            var extension =
                Path.GetExtension(image.FileName)
                    .ToLowerInvariant();

            // Generate nama unik
            var fileName =
                $"layanan-{Guid.NewGuid()}{extension}";

            var filePath = Path.Combine(
                folderPath,
                fileName
            );

            // Simpan file
            using var stream = new FileStream(
                filePath,
                FileMode.Create
            );

            await image.CopyToAsync(stream);

            // Path yang disimpan ke database
            return $"/images/layanan/{fileName}";
        }


        // ==========================================
        // DELETE IMAGE
        // ==========================================

        private void DeleteImage(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                return;
            }

            var fileName =
                Path.GetFileName(imagePath);

            var filePath = Path.Combine(
                environment.WebRootPath,
                "images",
                "layanan",
                fileName
            );

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}