using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Katalog.Models;
using Katalog.Services;

namespace Katalog.Controller
{
    public static class PaymentController
    {
        public static void MapPayment(this WebApplication app)
        {
            var payment = app.MapGroup("api/v1/payment");

            // =========================================================
            // CREATE PAYMENT (SNAP TOKEN)
            //
            // POST /api/v1/payment/{idOrder}
            //
            // Authorization: Bearer <token>
            //
            // Response berisi Snap Token untuk dibuka di frontend.
            // Server Key TIDAK pernah dikirim ke frontend.
            // =========================================================

            payment.MapPost("/{idPesanan}", async (
                PaymentServices service,
                int idPesanan,
                HttpContext httpContext) =>
            {
                try
                {
                    var idUser = GetUserId(httpContext);

                    if (idUser == null)
                    {
                        return Results.Unauthorized();
                    }

                    var result = await service.CreatePaymentAsync(
                        idPesanan,
                        idUser.Value);

                    if (!result.Success)
                    {
                        return Results.BadRequest(new
                        {
                            message = result.Message
                        });
                    }

                    return Results.Ok(result.Data);
                }
                catch (MidtransException e)
                {
                    return Results.Problem(
                        title: "Midtrans Error",
                        statusCode: 502,
                        detail: e.Message
                    );
                }
                catch (Exception e)
                {
                    return Results.Problem(
                        title: "Internal Server Error",
                        statusCode: 500,
                        detail: e.Message
                    );
                }
            }).RequireAuthorization(Policies.AdminPetugasPelanggan);

            // =========================================================
            // GET PAYMENT BY ORDER
            //
            // GET /api/v1/payment/pesanan/{idPesanan}
            //
            // Authorization: Bearer <token>
            // =========================================================

            payment.MapGet("/pesanan/{idPesanan}", async (
                PaymentServices service,
                int idPesanan,
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
                        await service.GetPaymentByPesananAsync(
                            idPesanan,
                            idUser.Value);

                    if (result == null)
                    {
                        return Results.NotFound(new
                        {
                            message = "Pembayaran tidak ditemukan"
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
            }).RequireAuthorization(Policies.AdminPetugasPelanggan);

            // =========================================================
            // MIDTRANS NOTIFICATION / WEBHOOK
            //
            // POST /api/v1/payment/midtrans/notification
            //
            // Endpoint ini publik karena dipanggil oleh server Midtrans.
            // Keamanan dijamin melalui validasi signature_key.
            // =========================================================

            payment.MapPost("/midtrans/notification", async (
                PaymentServices service,
                MidtransNotification notification) =>
            {
                try
                {
                    var result =
                        await service.HandleNotificationAsync(
                            notification);

                    if (result.InvalidSignature)
                    {
                        return Results.Json(
                            new { message = result.Message },
                            statusCode: StatusCodes.Status401Unauthorized);
                    }

                    return Results.Ok(new
                    {
                        message = result.Message
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
            }).AllowAnonymous();
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
