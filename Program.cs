using System.Diagnostics;
using Katalog.Services;
using Katalog.Controller;
using Katalog.Models;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAndroid", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });

    options.AddPolicy("AllowWebFrontend", policy =>
    {
        policy.WithOrigins(
                    "http://localhost:3000",
                    "http://localhost:5174",
                    "http://localhost:4200",
                    "https://yourdomain.com",
                    "http://127.0.0.1:5174"
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });

    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});
//ENV
Env.Value = builder.Configuration;
//Database Services
builder.Services.AddSingleton<Database>();

//Auth Services
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddScoped<IJWTService, JWTService>();

//KategoryProduct Services
builder.Services.AddScoped<KategoryProductServices>();

//StatusProduct Services
builder.Services.AddScoped<StatusProductServices>();

//StatusPengerjaan Services
builder.Services.AddScoped<StatusPengerjaanServices>();

//Product Services
builder.Services.AddScoped<ProductServices>();

//Alamat Services
builder.Services.AddScoped<AlamatServices>();

//Layanan Services
builder.Services.AddScoped<LayananServices>();

//Pesanan Services
builder.Services.AddScoped<PesananServices>();

//Midtrans & Payment Services
builder.Services.AddHttpClient<MidtransService>();
builder.Services.AddScoped<PaymentServices>();


//JWT
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
//Swagger

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Percetakan",
        Version = "v1",
        Description = "Dokumentasi API Percetakan"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Masukkan JWT Token saja. Swagger akan otomatis menambahkan kata 'Bearer ' di depannya.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
//Register Services

builder.Services.AddAuthorization(Policies.Register);
builder.Services.AddScoped<AuthServices>();
var app = builder.Build();

app.UseStaticFiles();

app.UseCors("AllowWebFrontend");
//app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    var sw = Stopwatch.StartNew();
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
        throw;
    }
    finally
    {
        sw.Stop();
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        Console.WriteLine($"{timestamp} INFO: {ip} - \"{context.Request.Method} {context.Request.Path} {context.Response.StatusCode}\" {sw.ElapsedMilliseconds}ms");
    }
});
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Katalog API v1");
});

app.MapAuth();
app.MapKategoryProduct();
app.MapStatusProduct();
app.MapStatusPengerjaan();
app.MapProduct();
app.MapAlamat();
app.MapLayanan();
app.MapPesanan();
app.MapPayment();


app.Run();
