using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Katalog.Models;
using Katalog.Services;

namespace Katalog.Controller
{
    public static class PesananController
    {
        public static void MapPesanan(this WebApplication app)
        {
            var pesanan = app
                .MapGroup("api/v1/pesanan")
                .RequireAuthorization(Policies.AdminPetugasPelanggan);

            // =========================================================
            // GET ALL PESANAN
            // ADMIN & PETUGAS
            //
            // GET /api/v1/pesanan/all
            // =========================================================

            pesanan.MapGet("/all", async (
                PesananServices service) =>
            {
                try
                {
                    var result =
                        await service.GetAllPesananAsync();

                    return Results.Ok(result);
                }
                catch (Exception e)
                {
                    return Results.Problem(
                        title: "Internal Server Error",
                        statusCode: 500,
                        detail: e.Message
                    );
                }
            }).RequireAuthorization(Policies.AdminPetugas);

            // =========================================================
            // GET PESANAN USER SENDIRI
            //
            // GET /api/v1/pesanan
            // =========================================================

            pesanan.MapGet("/", async (
                PesananServices service,
                HttpContext httpContext) =>
            {
                try
                {
                    var idUser = GetUserId(httpContext);

                    if (idUser == null)
                    {
                        return Results.Unauthorized();
                    }

                    var result =
                        await service.GetPesananByUserAsync(
                            idUser.Value);

                    return Results.Ok(result);
                }
                catch (Exception e)
                {
                    return Results.Problem(
                        title: "Internal Server Error",
                        statusCode: 500,
                        detail: e.Message
                    );
                }
            });

            // =========================================================
            // GET PESANAN BY ID
            //
            // GET /api/v1/pesanan/{id}
            //
            // Pelanggan hanya bisa melihat miliknya sendiri.
            // Admin & Petugas bisa melihat semua.
            // =========================================================

            pesanan.MapGet("/{id}", async (
                PesananServices service,
                int id,
                HttpContext httpContext) =>
            {
                try
                {
                    var idUser = GetUserId(httpContext);

                    if (idUser == null)
                    {
                        return Results.Unauthorized();
                    }

                    var isStaff =
                        httpContext.User.IsInRole("Admin")
                        || httpContext.User.IsInRole("Petugas");

                    var result = await service.GetPesananByIdAsync(
                        id,
                        isStaff ? null : idUser.Value);

                    if (result == null)
                    {
                        return Results.NotFound(new
                        {
                            message = "Pesanan tidak ditemukan"
                        });
                    }

                    return Results.Ok(result);
                }
                catch (Exception e)
                {
                    return Results.Problem(
                        title: "Internal Server Error",
                        statusCode: 500,
                        detail: e.Message
                    );
                }
            });

            // =========================================================
            // CREATE PESANAN
            //
            // POST /api/v1/pesanan
            //
            // Content-Type: multipart/form-data (WAJIB, bukan JSON).
            // Setiap parameter dikirim sebagai form field terpisah:
            //
            // idAlamat: 1
            // items[0].idProduct: 1
            // items[0].idUkuranProduk:
            // items[0].ukuranCustom: A3
            // items[0].qty: 2
            // items[0].notes: Cetak warna
            // items[0].desain: (file, opsional)
            // items[0].desainText: Selamat Ulang Tahun
            //
            // File desain dikirim sebagai multipart file field
            // items[N].desain, BUKAN Base64 / JSON. Boundary dibuat
            // otomatis oleh HTTP client/library.
            //
            // idUser DIAMBIL DARI JWT, bukan dari body.
            // =========================================================

            pesanan.MapPost("/", async (
                PesananServices service,
                IWebHostEnvironment environment,
                HttpContext httpContext) =>
            {
                try
                {
                    var idUser = GetUserId(httpContext);

                    if (idUser == null)
                    {
                        return Results.Unauthorized();
                    }

                    // Endpoint ini HANYA menerima multipart/form-data.
                    var request = await ParsePesananFormAsync(httpContext);

                    if (request == null)
                    {
                        return Results.BadRequest(new
                        {
                            message = "Content-Type harus multipart/form-data"
                        });
                    }

                    await SaveDesainFilesAsync(
                        environment,
                        request.Items);

                    var validation = await ValidateAsync(
                        service,
                        request,
                        idUser.Value);

                    if (validation != null)
                    {
                        return validation;
                    }

                    var result = await service.CreatePesananAsync(
                        idUser.Value,
                        request);

                    if (result == null)
                    {
                        return Results.BadRequest(new
                        {
                            message = "Pesanan gagal dibuat"
                        });
                    }

                    return Results.Ok(result);
                }
                catch (Exception e)
                {
                    return Results.Problem(
                        title: "Internal Server Error",
                        statusCode: 500,
                        detail: e.Message
                    );
                }
            }).DisableAntiforgery();

            // =========================================================
            // UPDATE PESANAN
            //
            // PUT /api/v1/pesanan/{id}
            //
            // Content-Type: multipart/form-data (WAJIB, bukan JSON).
            // Field form sama seperti POST /api/v1/pesanan.
            //
            // Hanya pemilik dan hanya jika belum ada pembayaran.
            // =========================================================

            pesanan.MapPut("/{id}", async (
                PesananServices service,
                IWebHostEnvironment environment,
                int id,
                HttpContext httpContext) =>
            {
                try
                {
                    var idUser = GetUserId(httpContext);

                    if (idUser == null)
                    {
                        return Results.Unauthorized();
                    }

                    // Endpoint ini HANYA menerima multipart/form-data.
                    var request = await ParsePesananFormAsync(httpContext);

                    if (request == null)
                    {
                        return Results.BadRequest(new
                        {
                            message = "Content-Type harus multipart/form-data"
                        });
                    }

                    await SaveDesainFilesAsync(
                        environment,
                        request.Items);

                    var validation = await ValidateAsync(
                        service,
                        request,
                        idUser.Value);

                    if (validation != null)
                    {
                        return validation;
                    }

                    var result = await service.UpdatePesananAsync(
                        id,
                        idUser.Value,
                        request);

                    if (result == null)
                    {
                        return Results.BadRequest(new
                        {
                            message = "Pesanan tidak ditemukan atau sudah memiliki pembayaran"
                        });
                    }

                    return Results.Ok(result);
                }
                catch (Exception e)
                {
                    return Results.Problem(
                        title: "Internal Server Error",
                        statusCode: 500,
                        detail: e.Message
                    );
                }
            }).DisableAntiforgery();

            // =========================================================
            // DELETE PESANAN
            //
            // DELETE /api/v1/pesanan/{id}
            //
            // Hanya pemilik dan hanya jika belum ada pembayaran.
            // =========================================================

            pesanan.MapDelete("/{id}", async (
                PesananServices service,
                int id,
                HttpContext httpContext) =>
            {
                try
                {
                    var idUser = GetUserId(httpContext);

                    if (idUser == null)
                    {
                        return Results.Unauthorized();
                    }

                    var success =
                        await service.DeletePesananAsync(
                            id,
                            idUser.Value);

                    if (!success)
                    {
                        return Results.BadRequest(new
                        {
                            message = "Pesanan tidak ditemukan atau sudah memiliki pembayaran"
                        });
                    }

                    return Results.Ok(new
                    {
                        message = "Pesanan berhasil dihapus"
                    });
                }
                catch (Exception e)
                {
                    return Results.Problem(
                        title: "Internal Server Error",
                        statusCode: 500,
                        detail: e.Message
                    );
                }
            });
        }

        // =========================================================
        // VALIDASI REQUEST PESANAN
        // =========================================================
        private static async Task<IResult?> ValidateAsync(
            PesananServices service,
            PesananRequest request,
            int idUser)
        {
            if (request.IdAlamat <= 0)
            {
                return Results.BadRequest(new
                {
                    message = "IdAlamat wajib diisi"
                });
            }

            var alamatExists = await service.AlamatExistsAsync(
                request.IdAlamat,
                idUser);

            if (!alamatExists)
            {
                return Results.BadRequest(new
                {
                    message = "Alamat tidak ditemukan"
                });
            }

            if (request.Items == null || request.Items.Count == 0)
            {
                return Results.BadRequest(new
                {
                    message = "Pesanan minimal memiliki 1 item"
                });
            }

            foreach (var item in request.Items)
            {
                if (item.IdProduct <= 0)
                {
                    return Results.BadRequest(new
                    {
                        message = "IdProduct wajib diisi"
                    });
                }

                if (item.Qty <= 0)
                {
                    return Results.BadRequest(new
                    {
                        message = "Qty minimal 1"
                    });
                }

                var productExists =
                    await service.ProductExistsAsync(item.IdProduct);

                if (!productExists)
                {
                    return Results.BadRequest(new
                    {
                        message = $"Produk {item.IdProduct} tidak ditemukan"
                    });
                }

                if (item.IdUkuranProduk.HasValue)
                {
                    var ukuranValid =
                        await service.UkuranBelongsToProductAsync(
                            item.IdUkuranProduk.Value,
                            item.IdProduct);

                    if (!ukuranValid)
                    {
                        return Results.BadRequest(new
                        {
                            message = $"Ukuran tidak sesuai dengan produk {item.IdProduct}"
                        });
                    }
                }
            }

            return null;
        }

        // =========================================================
        // AMBIL idUser DARI JWT
        // =========================================================
        private static int? GetUserId(HttpContext httpContext)
        {
            var idUserClaim =
                httpContext.User.FindFirst(
                    JwtRegisteredClaimNames.Sub)
                ??
                httpContext.User.FindFirst(
                    ClaimTypes.NameIdentifier);

            if (idUserClaim == null)
            {
                return null;
            }

            if (!int.TryParse(idUserClaim.Value, out var idUser))
            {
                return null;
            }

            return idUser;
        }

        // =========================================================
        // MULTIPART FORM PARSING
        //
        // POST/PUT /api/v1/pesanan HANYA menerima multipart/form-data.
        // JSON TIDAK diterima. Setiap parameter dikirim sebagai form
        // field terpisah dengan nama yang sama seperti di
        // PesananRequest:
        //
        //   idAlamat, items[N].idProduct, items[N].idUkuranProduk,
        //   items[N].ukuranCustom, items[N].qty, items[N].notes,
        //   items[N].desainText
        //
        // File desain dikirim sebagai multipart file field
        // items[N].desain (bukan Base64, bukan JSON).
        // Boundary TIDAK diset manual — dibuat otomatis oleh
        // HTTP client/library.
        // =========================================================
        private const int MaxItems = 50;

        private static async Task<PesananRequest?> ParsePesananFormAsync(
            HttpContext httpContext)
        {
            if (!httpContext.Request.HasFormContentType)
            {
                return null;
            }

            var form =
                await httpContext.Request.ReadFormAsync();

            var request = new PesananRequest
            {
                IdAlamat = ParseInt(form["idAlamat"]) ?? 0,
                Items = new List<PesananDetailRequest>()
            };

            // Item diindex mulai dari 0:
            // items[0].idProduct, items[1].qty, dst.
            for (var i = 0; i < MaxItems; i++)
            {
                var prefix = $"items[{i}].";

                var idProduct =
                    ParseInt(form[$"{prefix}idProduct"]);

                // Tidak ada field items[i].idProduct =
                // item terakhir sudah tercapai.
                if (idProduct == null)
                {
                    break;
                }

                var item = new PesananDetailRequest
                {
                    IdProduct = idProduct.Value,
                    IdUkuranProduk =
                        ParseInt(form[$"{prefix}idUkuranProduk"]),
                    UkuranCustom =
                        form[$"{prefix}ukuranCustom"],
                    Qty = ParseInt(form[$"{prefix}qty"]) ?? 0,
                    Notes = form[$"{prefix}notes"],
                    DesainText = form[$"{prefix}desainText"]
                };

                item.Desain =
                    form.Files.GetFile($"{prefix}desain");

                request.Items.Add(item);
            }

            return request;
        }

        private static int? ParseInt(string? value)
        {
            return int.TryParse(value, out var result)
                ? result
                : null;
        }

        // =========================================================
        // SIMPAN FILE DESAIN
        //
        // File multipart (items[N].desain) disimpan ke
        // wwwroot/images/desain, lalu path-nya diset ke
        // item.DesainFilePath.
        // Desain TIDAK diterima sebagai Base64 / JSON.
        // =========================================================
        private static async Task SaveDesainFilesAsync(
            IWebHostEnvironment environment,
            List<PesananDetailRequest> items)
        {
            Console.WriteLine($"[DEBUG] Total Items: {items.Count}");
        
            foreach (var item in items)
            {
                var desain = item.Desain;
        
                if (desain == null || desain.Length == 0)
                {
                    Console.WriteLine("[DEBUG] File Desain NULL atau Kosong!");
                    continue;
                }
        
                Console.WriteLine($"[DEBUG] File Diterima: {desain.FileName}, Size: {desain.Length} bytes");
        
                var rootPath = !string.IsNullOrEmpty(environment.WebRootPath)
                    ? environment.WebRootPath
                    : Path.Combine(environment.ContentRootPath, "wwwroot");
        
                var folderPath = Path.Combine(rootPath, "images", "desain");
        
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
        
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(desain.FileName);
                var filePath = Path.Combine(folderPath, fileName);
        
                await using var stream = new FileStream(filePath, FileMode.Create);
                await desain.CopyToAsync(stream);
        
                item.DesainFilePath = "/images/desain/" + fileName;
                Console.WriteLine($"[DEBUG] File Berhasil Disimpan ke: {filePath}");
            }
        }
    }
}
