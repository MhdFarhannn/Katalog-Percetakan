using Katalog.Services;
using Katalog.Models;
using Microsoft.AspNetCore.Mvc;

namespace Katalog.Controller
{
    public static class ProductController
    {
        public static void MapProduct(this WebApplication app)
        {
            var g = app.MapGroup("api/v1/products");

            // MENAMBAHKAN PRODUCT
            g.MapPost("/", async (
                ProductServices service,
                IWebHostEnvironment environment,
                [FromForm] int IdKategoriProduct,
                [FromForm] int IdStatusProduct,
                [FromForm] string Nama,
                [FromForm] string? Deskripsi,
                [FromForm] decimal Harga,
                [FromForm] string? BackgroundColor,
                IFormFile? Image) =>
            {
                try
                {
                    string? imagePath = null;
            
                    if (Image != null)
                    {
                        var extension = Path.GetExtension(Image.FileName);
                        var fileName = Guid.NewGuid().ToString() + extension;
                        var folderPath = Path.Combine(environment.WebRootPath, "images");
            
                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }
            
                        var filePath = Path.Combine(folderPath, fileName);
            
                        using var stream = new FileStream(filePath, FileMode.Create);
                        await Image.CopyToAsync(stream);
            
                        imagePath = "/images/" + fileName;
                    }
            
                    var product = new Product
                    {
                        IdKategoriProduct = IdKategoriProduct,
                        IdStatusProduct = IdStatusProduct,
                        Nama = Nama,
                        Deskripsi = Deskripsi,
                        ImagePath = imagePath,
                        Harga = Harga,
                        BackgroundColor = BackgroundColor
                    };
                    
                    var result = await service.AddProductAsync(product);
            
                    if (!result)
                    {
                        return Results.BadRequest();
                    }
            
                    return Results.Ok(product);
                }
                catch (Exception e)
                {
                    return Results.Problem(
                        title: "Internal Server Error",
                        statusCode: 500,
                        detail: e.Message
                    );
                }
            }).DisableAntiforgery().RequireAuthorization(Policies.Admin);


            // GET ALL PRODUCT
            g.MapGet("/", async (
                ProductServices service) =>
            {
                try
                {
                    var products =
                        await service.GetAllProductsAsync();

                    return Results.Ok(products);
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

            // // GET PRODUCT BY ID
            // g.MapGet("/{id}", async (
            //     ProductServices service,
            //     int id) =>
            // {
            //     try
            //     {
            //         var product =
            //             await service.GetProductByIdAsync(id);

            //         if (product == null)
            //         {
            //             return Results.NotFound();
            //         }

            //         return Results.Ok(product);
            //     }
            //     catch (Exception e)
            //     {
            //         return Results.Problem(
            //             title: "Internal Server Error",
            //             statusCode: 500,
            //             detail: e.Message
            //         );
            //     }
            // });

            // EDIT PRODUCT
            g.MapPatch("/{id}", async (
                ProductServices service,
                int id,
                Product product) =>
            {
                try
                {
                    var result =
                        await service.UpdateProductAsync(id, product);

                    if (!result)
                    {
                        return Results.NotFound();
                    }

                    return Results.Ok();
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

            // DELETE PRODUCT
            g.MapDelete("/{id}", async (
                ProductServices service,
                int id) =>
            {
                try
                {
                    var result =
                        await service.DeleteProductAsync(id);

                    if (!result)
                    {
                        return Results.NotFound();
                    }

                    return Results.Ok();
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
