using Katalog.Services;
using Katalog.Models;

namespace Katalog.Controller
{
    public static class KategoryProductController
    {
        public static void MapKategoryProduct(this WebApplication app)
        {
            var g = app.MapGroup("/kategory-product");
            
            //MENAMBAHKAN KATEGORY PRODUCT
            g.MapPost("/", async (KategoryProductServices service, KategoryProduct kategoryProduct) =>
            {
                try
                {
                    await service.CreateKategoryProduct(kategoryProduct);
                    return Results.Ok();
                }
                catch (Exception e)
                {
                     return Results.Problem(title: "Internal Server Error", statusCode: 500, detail: e.Message);
                }
            });

            //MENGHAPUS KATEGORY PRODUCT
            g.MapDelete("/{id}", async (KategoryProductServices service, int id) =>
            {
                try
                {
                    await service.DeleteKategoryProduct(id);
                    return Results.Ok();
                }
                catch (Exception e)
                {
                    return Results.Problem(title: "Internal Server Error", statusCode: 500, detail: e.Message);
                }
            });

            //EDIT KATEGORY PRODUCT
            g.MapPatch("/{id}", async (KategoryProductServices service, int id, KategoryProduct kategoryProduct) =>
            {
                try
                {
                    await service.PatchKategoryProduct(id, kategoryProduct);
                    return Results.Ok();
                }
                catch (Exception e)
                {
                    return Results.Problem(title: "Internal Server Error", statusCode: 500, detail: e.Message);
                }
            });

            //GET ALL KATEGORI PRODUCT
            g.MapGet("/", async (KategoryProductServices service) =>
            {
                try
                {
                    var kategoryProducts = await service.GetAllKategoryProduct();
                    return Results.Ok(kategoryProducts);
                }
                catch (Exception e)
                {
                    return Results.Problem(title: "Internal Server Error", statusCode: 500, detail: e.Message);
                }
            });
        }
    }
}
