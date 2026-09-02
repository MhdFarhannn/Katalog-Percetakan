using Katalog.Models;
using Katalog.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Katalog.Controller
{
    public static class AlamatController
    {
        public static void MapAlamat(this WebApplication app)
        {
            var alamat = app
                .MapGroup("api/v1/alamat")
                .RequireAuthorization(
                    Policies.AdminPetugasPelanggan
                );

            // =========================================================
            // GET ALL ALAMAT
            // ADMIN & PETUGAS
            //
            // GET /api/v1/alamat/all
            // =========================================================

            alamat.MapGet("/all", async (
                AlamatServices service) =>
            {
                try
                {
                    var result =
                        await service.GetAllAlamatAsync();

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
            // GET ALAMAT BY USER
            // ADMIN & PETUGAS
            //
            // GET /api/v1/alamat/user/{idUser}
            // =========================================================

            alamat.MapGet("/user/{idUser}", async (
                AlamatServices service,
                int idUser) =>
            {
                try
                {
                    var result =
                        await service.GetAlamatByUserAsync(
                            idUser
                        );

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
            // GET ALAMAT USER SENDIRI
            //
            // GET /api/v1/alamat
            //
            // Authorization: Bearer <token>
            //
            // idUser diambil dari JWT
            // =========================================================

            alamat.MapGet("/", async (
                AlamatServices service,
                HttpContext httpContext) =>
            {
                try
                {
                    var idUserClaim =
                        httpContext.User.FindFirst(
                            JwtRegisteredClaimNames.Sub
                        )
                        ??
                        httpContext.User.FindFirst(
                            ClaimTypes.NameIdentifier
                        );

                    if (idUserClaim == null)
                    {
                        return Results.Unauthorized();
                    }

                    if (!int.TryParse(
                        idUserClaim.Value,
                        out var idUser))
                    {
                        return Results.Unauthorized();
                    }

                    var result =
                        await service.GetAlamatByUserAsync(
                            idUser
                        );

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
            // POST ALAMAT
            // PELANGGAN
            //
            // POST /api/v1/alamat
            //
            // Authorization: Bearer <token>
            //
            // Body:
            // {
            //     "content": "Jl. Merdeka No. 10",
            //     "noTelepon": "081234567890"
            // }
            //
            // idUser TIDAK DIAMBIL DARI BODY.
            // idUser DIAMBIL DARI JWT.
            // =========================================================

            alamat.MapPost("/", async (
                AlamatServices service,
                AlamatRequest request,
                HttpContext httpContext) =>
            {
                try
                {
                    var idUserClaim =
                        httpContext.User.FindFirst(
                            JwtRegisteredClaimNames.Sub
                        )
                        ??
                        httpContext.User.FindFirst(
                            ClaimTypes.NameIdentifier
                        );

                    if (idUserClaim == null)
                    {
                        return Results.Unauthorized();
                    }

                    if (!int.TryParse(
                        idUserClaim.Value,
                        out var idUser))
                    {
                        return Results.Unauthorized();
                    }

                    // Validasi content
                    if (string.IsNullOrWhiteSpace(
                        request.Content))
                    {
                        return Results.BadRequest(new
                        {
                            message = "Content alamat wajib diisi"
                        });
                    }

                    // Validasi nomor telepon
                    if (string.IsNullOrWhiteSpace(
                        request.NoTelepon))
                    {
                        return Results.BadRequest(new
                        {
                            message = "NoTelepon wajib diisi"
                        });
                    }

                    var result =
                        await service.AddAlamatAsync(
                            idUser,
                            request
                        );

                    if (result == null)
                    {
                        return Results.BadRequest(new
                        {
                            message = "Alamat gagal ditambahkan"
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
            // PATCH ALAMAT
            // PELANGGAN
            //
            // PATCH /api/v1/alamat/{id}
            //
            // Authorization: Bearer <token>
            //
            // Body:
            // {
            //     "content": "Alamat baru",
            //     "noTelepon": "081234567890"
            // }
            //
            // User hanya bisa mengubah alamat miliknya sendiri.
            // =========================================================

            alamat.MapPatch("/{id}", async (
                AlamatServices service,
                int id,
                AlamatRequest request,
                HttpContext httpContext) =>
            {
                try
                {
                    var idUserClaim =
                        httpContext.User.FindFirst(
                            JwtRegisteredClaimNames.Sub
                        )
                        ??
                        httpContext.User.FindFirst(
                            ClaimTypes.NameIdentifier
                        );

                    if (idUserClaim == null)
                    {
                        return Results.Unauthorized();
                    }

                    if (!int.TryParse(
                        idUserClaim.Value,
                        out var idUser))
                    {
                        return Results.Unauthorized();
                    }

                    // Validasi content
                    if (string.IsNullOrWhiteSpace(
                        request.Content))
                    {
                        return Results.BadRequest(new
                        {
                            message = "Content alamat wajib diisi"
                        });
                    }

                    // Validasi nomor telepon
                    if (string.IsNullOrWhiteSpace(
                        request.NoTelepon))
                    {
                        return Results.BadRequest(new
                        {
                            message = "NoTelepon wajib diisi"
                        });
                    }

                    var success =
                        await service.UpdateAlamatAsync(
                            id,
                            idUser,
                            request
                        );

                    if (!success)
                    {
                        return Results.NotFound(new
                        {
                            message = "Alamat tidak ditemukan"
                        });
                    }

                    // Ambil data terbaru
                    var result =
                        await service.GetAlamatByIdAsync(
                            id,
                            idUser
                        );

                    if (result == null)
                    {
                        return Results.NotFound(new
                        {
                            message = "Alamat tidak ditemukan"
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
            // DELETE ALAMAT
            // PELANGGAN
            //
            // DELETE /api/v1/alamat/{id}
            //
            // Authorization: Bearer <token>
            //
            // User hanya bisa menghapus alamat miliknya sendiri.
            // =========================================================

            alamat.MapDelete("/{id}", async (
                AlamatServices service,
                int id,
                HttpContext httpContext) =>
            {
                try
                {
                    var idUserClaim =
                        httpContext.User.FindFirst(
                            JwtRegisteredClaimNames.Sub
                        )
                        ??
                        httpContext.User.FindFirst(
                            ClaimTypes.NameIdentifier
                        );

                    if (idUserClaim == null)
                    {
                        return Results.Unauthorized();
                    }

                    if (!int.TryParse(
                        idUserClaim.Value,
                        out var idUser))
                    {
                        return Results.Unauthorized();
                    }

                    var success =
                        await service.DeleteAlamatAsync(
                            id,
                            idUser
                        );

                    if (!success)
                    {
                        return Results.NotFound(new
                        {
                            message = "Alamat tidak ditemukan"
                        });
                    }

                    return Results.Ok(new
                    {
                        message = "Alamat berhasil dihapus"
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
    }
}
