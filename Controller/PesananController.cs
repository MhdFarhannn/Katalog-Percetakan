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
            // Body:
            // {
            //     "idAlamat": 1,
            //     "items": [
            //         {
            //             "idProduct": 1,
            //             "idUkuranProduk": null,
            //             "ukuranCustom": "A3",
            //             "qty": 2,
            //             "notes": "Cetak warna",
            //             "desainText": "Selamat Ulang Tahun"
            //         }
            //     ]
            // }
            //
            // idUser DIAMBIL DARI JWT, bukan dari body.
            // =========================================================

            pesanan.MapPost("/", async (
                PesananServices service,
                PesananRequest request,
                HttpContext httpContext) =>
            {
                try
                {
                    var idUser = GetUserId(httpContext);

                    if (idUser == null)
                    {
                        return Results.Unauthorized();
                    }

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
            });

            // =========================================================
            // UPDATE PESANAN
            //
            // PUT /api/v1/pesanan/{id}
            //
            // Hanya pemilik dan hanya jika belum ada pembayaran.
            // =========================================================

            pesanan.MapPut("/{id}", async (
                PesananServices service,
                int id,
                PesananRequest request,
                HttpContext httpContext) =>
            {
                try
                {
                    var idUser = GetUserId(httpContext);

                    if (idUser == null)
                    {
                        return Results.Unauthorized();
                    }

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
            });

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
    }
}
