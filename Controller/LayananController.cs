using Katalog.Models;
using Katalog.Services;

namespace Katalog.Controller
{
    public static class LayananController
    {
        public static void MapLayanan(this WebApplication app)
        {
            var g = app
                .MapGroup("api/v1/layanan")
                .RequireAuthorization(Policies.AdminPetugasPelanggan);


            // ==========================================
            // GET ALL
            // ==========================================

            g.MapGet("/", async (
                LayananServices service) =>
            {
                try
                {
                    var result =
                        await service.GetLayananAsync();

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


            // ==========================================
            // GET BY ID
            // ==========================================

            g.MapGet("/{id}", async (
                LayananServices service,
                int id) =>
            {
                try
                {
                    var result =
                        await service.GetLayananByIdAsync(id);

                    if (result == null)
                    {
                        return Results.NotFound(new
                        {
                            message = "Layanan tidak ditemukan"
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


            // ==========================================
            // CREATE
            // ==========================================

            g.MapPost("/", async (
                LayananServices service,
                HttpRequest request) =>
            {
                try
                {
                    var form =
                        await request.ReadFormAsync();

                    var nama =
                        form["Nama"].ToString();

                    var deskripsi =
                        form["Deskripsi"].ToString();

                    var backgroundColor =
                        form["BackgroundColor"].ToString();

                    var image =
                        form.Files.GetFile("Image");


                    if (string.IsNullOrWhiteSpace(nama))
                    {
                        return Results.BadRequest(new
                        {
                            message = "Nama layanan wajib diisi"
                        });
                    }

                    if (string.IsNullOrWhiteSpace(deskripsi))
                    {
                        return Results.BadRequest(new
                        {
                            message = "Deskripsi layanan wajib diisi"
                        });
                    }


                    var layanan = new Layanan
                    {
                        Nama = nama,
                        Deskripsi = deskripsi,
                        BackgroundColor =
                            backgroundColor
                    };


                    var result =
                        await service.CreateLayananAsync(
                            layanan,
                            image
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


            // ==========================================
            // PATCH
            // ==========================================

            g.MapPatch("/{id}", async (
                LayananServices service,
                int id,
                HttpRequest request) =>
            {
                try
                {
                    var form =
                        await request.ReadFormAsync();

                    var nama =
                        form["Nama"].ToString();

                    var deskripsi =
                        form["Deskripsi"].ToString();

                    var backgroundColor =
                        form["BackgroundColor"].ToString();

                    var image =
                        form.Files.GetFile("Image");


                    if (string.IsNullOrWhiteSpace(nama))
                    {
                        return Results.BadRequest(new
                        {
                            message = "Nama layanan wajib diisi"
                        });
                    }

                    if (string.IsNullOrWhiteSpace(deskripsi))
                    {
                        return Results.BadRequest(new
                        {
                            message = "Deskripsi layanan wajib diisi"
                        });
                    }


                    var layanan = new Layanan
                    {
                        Id = id,
                        Nama = nama,
                        Deskripsi = deskripsi,
                        BackgroundColor =
                            backgroundColor
                    };


                    var result =
                        await service.PatchLayananAsync(
                            id,
                            layanan,
                            image
                        );


                    if (result == null)
                    {
                        return Results.NotFound(new
                        {
                            message = "Layanan tidak ditemukan"
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


            // ==========================================
            // DELETE
            // ==========================================

            g.MapDelete("/{id}", async (
                LayananServices service,
                int id) =>
            {
                try
                {
                    var result =
                        await service.DeleteLayananAsync(id);


                    if (!result)
                    {
                        return Results.NotFound(new
                        {
                            message = "Layanan tidak ditemukan"
                        });
                    }


                    return Results.Ok(new
                    {
                        message =
                            "Layanan berhasil dihapus"
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
