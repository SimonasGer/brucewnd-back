using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using Amazon.S3;
using Amazon;
using Npgsql;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IAmazonS3>(_ =>
    new AmazonS3Client(RegionEndpoint.EUNorth1)); 
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));


builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentName = "BruceWnD";
    config.Title = "BruceWnD v1";
    config.Version = "v1";
});

var allowedOrigin = "http://localhost:3000";

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendOnly", policy =>
    {
        policy.WithOrigins(allowedOrigin)
            .AllowAnyHeader()
            .WithMethods("GET","POST","PUT","PATCH","DELETE","OPTIONS")
            .AllowCredentials();
    });
});

var app = builder.Build();


// Program.cs (after building the app)
using (var scope = app.Services.CreateScope())
{
    var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Optional: longer command timeout for migrations
    db.Database.SetCommandTimeout(TimeSpan.FromSeconds(60));

    // Only auto-migrate in Development (recommended)
    if (env.IsDevelopment())
    {
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await db.Database.OpenConnectionAsync(); // fail fast if handshake borks
            try
            {
                var pending = await db.Database.GetPendingMigrationsAsync();
                if (pending.Any())
                {
                    logger.LogInformation("Applying {Count} pending migrations...", pending.Count());
                    await db.Database.MigrateAsync();
                    logger.LogInformation("Migrations applied.");
                }
                else
                {
                    logger.LogInformation("No pending migrations.");
                }
            }
            finally
            {
                await db.Database.CloseConnectionAsync();
            }
        });
    }
    else
    {
        // In Production, prefer running migrations out-of-band:
        // dotnet ef database update
        logger.LogInformation("Skipping auto-migrate (non-Development environment).");
    }
}

app.UseCors("FrontendOnly");
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi(config =>
    {
        config.DocumentTitle = "BruceWnD";
        config.Path = "/swagger";
        config.DocumentPath = "/swagger/{documentName}/swagger.json";
        config.DocExpansion = "list";
    });
}

app.MapComicEndpoints();
app.MapUserEndpoints();

app.Run();
