using System.Text;
using Autopartspro.API.Filters;
using Autopartspro.API.Middleware;
using Autopartspro.Application.Interfaces;
using Autopartspro.Infrastructure.Data;
using Autopartspro.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Controllers (camelCase JSON for React frontend)
builder.Services.AddControllers(options =>
    {
        options.Filters.Add<ApiExceptionFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

//  Database (PostgreSQL) 
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration
        .GetConnectionString("DefaultConnection")));

// JWT Authentication 
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Keep claim types as-is so User.FindFirstValue(ClaimTypes.Email) works in controllers
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrEmpty(origin)) return false;
                var uri = new Uri(origin);
                return uri.Host is "localhost" or "127.0.0.1";
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

// DI Services 
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IOtpService, OtpService>();   // ← was missing
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ISalesInvoiceService, SalesInvoiceService>();
builder.Services.AddTransient<GlobalExceptionHandler>();
builder.Services.AddScoped<IAdminService, AdminService>();

// OpenAPI (.NET 10 built-in)
builder.Services.AddOpenApi();


var app = builder.Build();

// Global Exception Handler ─
app.UseMiddleware<GlobalExceptionHandler>();

// OpenAPI UI (dev only)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Middleware Pipeline
// Skip HTTPS redirect in Development — avoids "Network Error" when frontend calls http://localhost:5009
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }))
    .AllowAnonymous();

app.MapControllers();

// Auto-run migrations and ensure dev admin account
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    // Legacy enum value 2 (removed Overdue status) -> Unpaid
    await db.Database.ExecuteSqlRawAsync(
        """UPDATE "SalesInvoices" SET "PaymentStatus" = 1 WHERE "PaymentStatus" = 2""");
    await DevelopmentAdminBootstrap.EnsureAsync(
        db,
        scope.ServiceProvider.GetRequiredService<IConfiguration>(),
        app.Environment);
    await DevelopmentSampleDataBootstrap.EnsureAsync(db, app.Environment);
}

app.Run();