using Cleriq.Data;
using Cleriq.Helpers;
using Cleriq.Middleware;
using Cleriq.Models;
using Cleriq.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

Console.OutputEncoding = System.Text.Encoding.UTF8;

builder.Services.AddControllers();
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = ValidareDocument.MarimeMaxima;
});
builder.Services.AddOpenApi();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IFurnizorTenant, FurnizorTenant>();
builder.Services.AddScoped<IFurnizorUtilizator, FurnizorUtilizator>();

builder.Services.AddScoped<IGeneratorConvocare, GeneratorConvocare>();
builder.Services.AddScoped<IServiciuNotificare, NotificareLogger>();
builder.Services.AddSingleton<IStocareDocumente, StocareDocumenteDisk>();

builder.Services.AddHostedService<WorkerConvocari>();

// Cache pentru rezolvarea tenant-ului din slug pe rutele publice.
// SizeLimit ca centură de siguranță contra umflării (atacuri cu slug-uri random).
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 10000;
});

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
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Rol>>();
    foreach (var rol in new[] { "SuperAdmin", "Admin", "Secretar", "Consilier" })
    {
        if (!await roleManager.RoleExistsAsync(rol))
            await roleManager.CreateAsync(new Rol { Name = rol });
    }

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Utilizator>>();
    var emailSuperAdmin = app.Configuration["SuperAdmin:Email"];
    var parolaSuperAdmin = app.Configuration["SuperAdmin:Parola"];

    if (!string.IsNullOrWhiteSpace(emailSuperAdmin) && !string.IsNullOrWhiteSpace(parolaSuperAdmin))
    {
        if (await userManager.FindByEmailAsync(emailSuperAdmin) is null)
        {
            var superAdmin = new Utilizator
            {
                UserName = emailSuperAdmin,
                Email = emailSuperAdmin,
                NumeComplet = "Super Admin",
                InstitutieId = 0,
                EmailConfirmed = true
            };

            var rezultat = await userManager.CreateAsync(superAdmin, parolaSuperAdmin);
            if (rezultat.Succeeded)
                await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Middleware pentru rezolvarea tenant-ului din slug pe rutele publice.
// Rulează după auth (pe rute publice nu există claim oricum) și
// înainte de MapControllers, ca să seteze HttpContext.Items la timp.
app.UseMiddleware<SlugTenantMiddleware>();

app.MapControllers();

app.Run();