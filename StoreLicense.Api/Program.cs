
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using StoreLicense.Infrastructure.Data;
using StoreLicense.Core.Interfaces;
using StoreLicense.Infrastructure.Services;
using StoreLicense.Infrastructure.Security;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


// Data Protection - Activation Code Encryption
builder.Services
    .AddDataProtection()
    .SetApplicationName("StoreLicenseSystem")
    .PersistKeysToDbContext<AppDbContext>();




// Controllers
builder.Services.AddControllers();
// CORS - Admin Frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AdminFrontend", policy =>
    {
        policy
            .WithOrigins(
            "http://localhost:5173",
            "http://localhost:5174",
            "https://storelicense-admin.onrender.com")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter your JWT access token."
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});


// PostgreSQL Database
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Database connection string is not configured.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// Existing services
builder.Services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
builder.Services.AddScoped<ISubscriberService, SubscriberService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddSingleton<ActivationCodeProtectionService>();
builder.Services.AddScoped<IActivationService, ActivationService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<ILicenseValidationService, LicenseValidationService>();
builder.Services.AddSingleton<ILicenseTokenService, LicenseTokenService>();

// Admin services
builder.Services.AddScoped<AdminPasswordService>();
builder.Services.AddScoped<AdminTokenService>();

// Admin JWT Authentication
var adminJwtKey = builder.Configuration["AdminJwt:SecretKey"];
var adminJwtIssuer = builder.Configuration["AdminJwt:Issuer"];
var adminJwtAudience = builder.Configuration["AdminJwt:Audience"];

if (string.IsNullOrWhiteSpace(adminJwtKey) ||
    string.IsNullOrWhiteSpace(adminJwtIssuer) ||
    string.IsNullOrWhiteSpace(adminJwtAudience))
{
    throw new InvalidOperationException(
        "Admin JWT configuration is incomplete.");
}

var signingKey = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(adminJwtKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = adminJwtIssuer,

            ValidateAudience = true,
            ValidAudience = adminJwtAudience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),

            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = System.Security.Claims.ClaimTypes.Name
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AdminFrontend");

// Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
