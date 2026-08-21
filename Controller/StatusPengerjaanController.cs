using Katalog.Services;
using Katalog.Models;

namespace Katalog.Controller
{
    public static class StatusPengerjaanController
    {
        public static void MapStatusPengerjaan(this WebApplication app)
        {
            var g = app.MapGroup("api/v1/status-pengerjaan");
            
            //MENAMBAHKAN STATUS PENGERJAAN
            g.MapPost("/", async (StatusPengerjaanServices service, StatusPengerjaan statusPengerjaan) =>
            {
                try
                {
                    await service.CreateStatusPengerjaan(statusPengerjaan);
                    return Results.Ok();
                }
                catch (Exception e)
                {
                     return Results.Problem(title: "Internal Server Error", statusCode: 500, detail: e.Message);
                }
            });

            //MENGHAPUS STATUS PENGERJAAN
            g.MapDelete("/{id}", async (StatusPengerjaanServices service, int id) =>
            {
                try
                {
                    await service.DeleteStatusPengerjaan(id);
                    return Results.Ok();
                }
                catch (Exception e)
                {
                    return Results.Problem(title: "Internal Server Error", statusCode: 500, detail: e.Message);
                }
            });

            //EDIT STATUS PENGERJAAN
            g.MapPatch("/{id}", async (StatusPengerjaanServices service, int id, StatusPengerjaan statusPengerjaan) =>
            {
                try
                {
                    await service.PatchStatusPengerjaan(id, statusPengerjaan);
                    return Results.Ok();
                }
                catch (Exception e)
                {
                    return Results.Problem(title: "Internal Server Error", statusCode: 500, detail: e.Message);
                }
            });

            //GET ALL STATUS PENGERJAAN
            g.MapGet("/", async (StatusPengerjaanServices service) =>
            {
                try
                {
                    var statusPengerjaans = await service.GetAllStatusPengerjaan();
                    return Results.Ok(statusPengerjaans);
                }
                catch (Exception e)
                {
                    return Results.Problem(title: "Internal Server Error", statusCode: 500, detail: e.Message);
                }
            });
        }
    }
}
