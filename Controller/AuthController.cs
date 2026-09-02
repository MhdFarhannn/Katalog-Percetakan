using Katalog.Services;
using Katalog.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Katalog.Controller
{
    public static class AuthController
    {
        public static void MapAuth(this WebApplication app)
        {
            var g = app.MapGroup("/api/v1/auth");

            g.MapPost("/register-admin", async (AuthServices services, UserCreate data, IPasswordService pServices) =>
            {
                try
                {
                    var Is_Registered = await services.AdminIsRegistered();
                    if (Is_Registered == true)
                    {
                        return Results.BadRequest("API Have Been Disabled");
                    }
                    data.Password = pServices.HashPassword(data.Password);
                    var res = await services.RegisterAdmin(data);
                    if (res == true)
                    {
                        return Results.Ok();
                    }
                    else
                    {
                        return Results.BadRequest();
                    }
                }
                catch (Exception e)
                {
                    return Results.InternalServerError(e.Message);
                }
            });

            g.MapPost("/google", async (AuthServices services, IJWTService jwtServices, GoogleLoginRequest req) =>
            {
                try
                {
                    var payload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(req.IdToken);

                    var user = await services.LoginOrRegisterGoogle(
                        externalId: payload.Subject,
                        email: payload.Email,
                        nama: payload.Name
                    );

                    if (user is null || !user.Is_Active)
                    {
                        return Results.Unauthorized();
                    }

                    var token = jwtServices.GenerateToken(user);
                    var refreshToken = jwtServices.GenerateRefreshToken();
                    await services.UpdateRefreshToken(refreshToken, DateTime.UtcNow.AddDays(20), user.Id);

                    return Results.Ok(new LoginResponse
                    {
                        Token = token,
                        RefreshToken = refreshToken,
                        Nama = user.Nama,
                        Role = user.Role
                    });
                }
                catch (Google.Apis.Auth.InvalidJwtException)
                {
                    return Results.Unauthorized();
                }
                catch (Exception e)
                {
                    return Results.InternalServerError(e.Message);
                }
            });

            g.MapPost("/login", async (AuthServices services, IPasswordService pServices, IJWTService jwtServices, LoginRequest login) =>
            {
                try
                {
                    var user = await services.Login(login);
                    if (user is null || !user.Is_Active)
                    {
                        return Results.Unauthorized();
                    }

                    if (user.Auth_Provider != "local" || string.IsNullOrEmpty(user.Password))
                    {
                        return Results.BadRequest("Akun ini terdaftar via Google. Silakan login dengan Google.");
                    }
                    if (!pServices.VerifyPassword(login.Password, user.Password))
                    {
                        return Results.Unauthorized();
                    }
                    var token = jwtServices.GenerateToken(user);
                    var refreshToken = jwtServices.GenerateRefreshToken();
                    await services.UpdateRefreshToken(refreshToken, DateTime.UtcNow.AddDays(20), user.Id);
                    return Results.Ok(new LoginResponse
                    {
                        Token = token,
                        RefreshToken = refreshToken,
                        Nama = user.Nama,
                        Role = user.Role
                    });
                }
                catch (Exception e)
                {
                    return Results.InternalServerError(e.Message);
                }
            });
            g.MapGet("/me", async (
                AuthServices service,
                HttpContext httpContext) =>
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
            
                var result = await service.GetMeAsync(idUser);
            
                if (result == null)
                {
                    return Results.NotFound(new
                    {
                        message = "User tidak ditemukan"
                    });
                }
            
                return Results.Ok(result);
            });

        }
    }
}
