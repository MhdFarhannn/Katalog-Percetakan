using Katalog.Services;
using Katalog.Models;

namespace Katalog.Controller
{
    public static class StatusProductController
    {
        public static void MapStatusProduct(this WebApplication app)
        {
            var g = app.MapGroup("api/v1/status-product");
            
            //MENAMBAHKAN STATUS PRODUCT
            g.MapPost("/", async (StatusProductServices service, StatusProduct statusProduct) =>
            {
                try
                {
                    await service.CreateStatusProduct(statusProduct);
                    return Results.Ok();
                }
                catch (Exception e)
                {
                     return Results.Problem(title: "Internal Server Error", statusCode: 500, detail: e.Message);
                }
            });

            //MENGHAPUS STATUS PRODUCT
            g.MapDelete("/{id}", async (StatusProductServices service, int id) =>
            {
                try
                {
                    await service.DeleteStatusProduct(id);
                    return Results.Ok();
                }
                catch (Exception e)
                {
                    return Results.Problem(title: "Internal Server Error", statusCode: 500, detail: e.Message);
                }
            });

            //EDIT STATUS PRODUCT
            g.MapPatch("/{id}", async (StatusProductServices service, int id, StatusProduct statusProduct) =>
            {
                try
                {
                    await service.PatchStatusProduct(id, statusProduct);
                    return Results.Ok();
                }
                catch (Exception e)
                {
                    return Results.Problem(title: "Internal Server Error", statusCode: 500, detail: e.Message);
                }
            });

            //GET ALL STATUS PRODUCT
            g.MapGet("/", async (StatusProductServices service) =>
            {
                try
                {
                    var statusProducts = await service.GetAllStatusProduct();
                    return Results.Ok(statusProducts);
                }
                catch (Exception e)
                {
                    return Results.Problem(title: "Internal Server Error", statusCode: 500, detail: e.Message);
                }
            });
        }
    }
}
