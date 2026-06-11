using Cleriq.Data;
using Cleriq.Helpers;
using Cleriq.Middleware;
using Cleriq.Models;
using Cleriq.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using StackExchange.Redis;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

Console.OutputEncoding = System.Text.Encoding.UTF8;

// QuestPDF Community License — gratuit sub 1M USD venit anual. Obligatoriu setat
// înainte de prima generare, altfel aruncă excepție.
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// Redis: dependență de infrastructură obligatorie, ca SQL Server (NU opțională ca Twilio).
// Fail-fast: dacă Redis nu răspunde la pornire, aplicația se oprește cu eroare clară —
// aici vor sta cheile Data Protection, deci aplicația nu poate funcționa corect fără el.
var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("ConnectionStrings:Redis lipsește din configurare.");
var redis = ConnectionMultiplexer.Connect(redisConnectionString);
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    var origini = builder.Configuration
        .GetSection("Cors:OriginiPermise")
        .Get<string[]>() ?? Array.Empty<string>();

    options.AddDefaultPolicy(policy =>
    {
        if (origini.Length > 0)
            policy.WithOrigins(origini)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
    });
});

// Cheile Data Protection în Redis: partajate între instanțe, supraviețuiesc redeploy-ului.
// Lista NU are TTL — la producție politica de evicție trebuie să fie volatile-* (vezi nota deploy).
builder.Services
    .AddDataProtection()
    .PersistKeysToStackExchangeRedis(redis, "cleriq:dataprotection-keys")
    .SetApplicationName("Cleriq");

builder.Services.AddSingleton<ICriptareSecreta, CriptareDataProtection>();

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = ValidareAudio.MarimeMaxima;
});
builder.Services.AddOpenApi();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IFurnizorTenant, FurnizorTenant>();
builder.Services.AddScoped<IFurnizorUtilizator, FurnizorUtilizator>();

builder.Services.AddScoped<IGeneratorConvocare, GeneratorConvocare>();
builder.Services.AddScoped<IGeneratorPdfProcesVerbal, GeneratorPdfProcesVerbal>();
builder.Services.AddScoped<NotificareLogger>();
builder.Services.AddScoped<IServiciuNotificareEmail, NotificareSmtp>();

var twilioConfigurat = !string.IsNullOrWhiteSpace(builder.Configuration["Twilio:AccountSid"]);
if (twilioConfigurat)
{
    builder.Services.AddSingleton<IServiciuNotificareSms, NotificareTwilio>();
}
else
{
    builder.Services.AddSingleton<IServiciuNotificareSms, NotificareLogger>();
}

builder.Services.AddSingleton<IStocareDocumente, StocareDocumenteDisk>();

builder.Services.AddSingleton<IStocareAudio, StocareAudioDisk>();

builder.Services.AddScoped<IGeneratorPromptTranscriere, GeneratorPromptTranscriere>();

var whisperConfigurat = !string.IsNullOrWhiteSpace(builder.Configuration["Whisper:UrlBaza"]);
if (whisperConfigurat)
{
    builder.Services
        .AddHttpClient<IServiciuTranscriere, TranscriereWhisperWrapper>(client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["Whisper:UrlBaza"]!);

            var apiKey = builder.Configuration["Whisper:ApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey))
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var timeoutOre = builder.Configuration.GetValue<int>("Whisper:TimeoutOre", 6);
            client.Timeout = TimeSpan.FromHours(Math.Max(1, timeoutOre));
        })
        .SetHandlerLifetime(TimeSpan.FromHours(6));

    builder.Services.AddHostedService<WorkerTranscrieri>();
}

builder.Services.AddHostedService<WorkerConvocari>();

// Cache distribuit (Redis) pentru rezolvarea tenant-ului din slug pe rutele publice.
// Partajat între instanțe; supraviețuiește restartului aplicației.
// InstanceName prefixează toate cheile — namespacing curat în Redis.
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.ConnectionMultiplexerFactory = () => Task.FromResult<IConnectionMultiplexer>(redis);
    options.InstanceName = "cleriq:";
});

builder.Services.AddSingleton<ILacatDistribuit, LacatDistribuitRedis>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services
    .AddIdentity<Utilizator, Rol>(options =>
    {
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

var jwt = builder.Configuration.GetSection("Jwt");
var cheieJwt = Encoding.UTF8.GetBytes(jwt["Key"]!);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(cheieJwt)
        };
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    if (app.Environment.IsProduction())
    {
        app.Logger.LogInformation("Production: aplic migrațiile pendinte...");
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        app.Logger.LogInformation("Migrațiile au fost aplicate.");
    }

    await SeedComun.RuleazaAsync(scope.ServiceProvider, app.Configuration, app.Logger);

    if (app.Environment.IsDevelopment())
        await SeedDevelopment.RuleazaAsync(scope.ServiceProvider, app.Logger);
}

if (!whisperConfigurat)
{
    app.Logger.LogWarning(
        "Whisper:UrlBaza nu este configurat. WorkerTranscrieri NU rulează. " +
        "Setează Whisper:UrlBaza în user-secrets sau env vars pentru a-l activa.");
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// Middleware pentru rezolvarea tenant-ului din slug pe rutele publice.
// Rulează după auth (pe rute publice nu există claim oricum) și
// înainte de MapControllers, ca să seteze HttpContext.Items la timp.
app.UseMiddleware<SlugTenantMiddleware>();

app.MapControllers();

app.Run();

public partial class Program { }