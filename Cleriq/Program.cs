using Cleriq.Data;
using Cleriq.Models;
using Cleriq.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
//using Microsoft.OpenApi;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IFurnizorTenant, FurnizorTenant>();
builder.Services.AddScoped<IFurnizorUtilizator, FurnizorUtilizator>();

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
