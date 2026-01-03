using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Services;
using GhanaHybridRentalApi.Endpoints;
using Microsoft.AspNetCore.Diagnostics; // added to support UseExceptionHandler mapping
using Microsoft.AspNetCore.HttpOverrides; // Respect X-Forwarded-* headers (proxies)

// Enable legacy timestamp behavior for Npgsql to handle DateTime properly with PostgreSQL
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Register DateTimeKindInterceptor
builder.Services.AddSingleton<DateTimeKindInterceptor>();

// Prevent host shutdown when background services throw unhandled exceptions in production
builder.Host.ConfigureHostOptions(opts =>
{
    opts.BackgroundServiceExceptionBehavior = Microsoft.Extensions.Hosting.BackgroundServiceExceptionBehavior.Ignore;
});

// Additional configuration: expose detailed errors if set in config
var exposeDetailedErrors = builder.Configuration.GetValue<bool>("Diagnostics:ExposeDetailedErrors", false);

// Basic configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=ghanarental;Username=postgres;Password=postgres";

builder.Services.AddDbContext<AppDbContext>((provider, options) =>
{
    options.UseNpgsql(connectionString);
    options.AddInterceptors(provider.GetRequiredService<DateTimeKindInterceptor>());
});

// Core services
builder.Services.AddScoped<IAppConfigService, AppConfigService>();
builder.Services.AddScoped<PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

// WhatsApp configuration
var useWhatsAppCloudApi = builder.Configuration.GetValue<bool>("WhatsApp:UseCloudApi");
if (useWhatsAppCloudApi)
{
    builder.Services.AddHttpClient<IWhatsAppSender, WhatsAppCloudApiSender>();
}
else
{
    builder.Services.AddScoped<IWhatsAppSender, FakeWhatsAppSender>();
}

// Email service - Azure primary with SMTP fallback in production, fake in development
if (builder.Environment.IsProduction())
{
    builder.Services.AddScoped<IEmailService, CompositeEmailService>();
}
else
{
    builder.Services.AddScoped<IEmailService, FakeEmailService>();
}

builder.Services.AddScoped<IVehicleAvailabilityService, VehicleAvailabilityService>();
builder.Services.AddHttpClient<IVehicleDataService, VehicleDataService>();

// File upload service - use Azure Blob Storage for production
var useCloudStorage = builder.Configuration.GetValue<bool>("AzureStorage:Enabled", true);
if (useCloudStorage)
{
    builder.Services.AddScoped<IFileUploadService, CloudFileUploadService>();
}
else
{
    builder.Services.AddScoped<IFileUploadService, LocalFileUploadService>();
}

builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<IMarketingEmailService, MarketingEmailService>();
builder.Services.AddScoped<IPromoCodeService, PromoCodeService>();
builder.Services.AddHostedService<NotificationJobProcessor>();
builder.Services.AddHostedService<DepositRefundNotificationService>();

// Payment services configuration
// Payment services: always register real providers. Configuration (keys, enabled flags) are read at runtime via IAppConfigService.
builder.Services.AddHttpClient();
builder.Services.AddScoped<IStripePaymentService, StripePaymentService>();
builder.Services.AddHttpClient<IPaystackPaymentService, PaystackPaymentService>();

// Refund service - processes deposit refunds via Paystack
builder.Services.AddHttpClient<IRefundService, RefundService>();

// Turnstile verification service for bot protection
builder.Services.AddHttpClient<ITurnstileService, TurnstileService>();

// Payout execution service
var useRealPayouts = builder.Configuration.GetValue<bool>("Payout:UseRealExecution");
if (useRealPayouts)
{
    builder.Services.AddHttpClient<IPayoutExecutionService, PayoutExecutionService>();
}
else
{
    builder.Services.AddScoped<IPayoutExecutionService, FakePayoutExecutionService>();
}

// JSON serializer settings: allow object cycles to be ignored to prevent JSON exceptions on nested entity graphs
builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

// CORS configuration - Allow specific origins with credentials support
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(
                "https://ryverental.com",
                "https://dashboard.ryverental.com",
                "http://localhost:8080",
                "http://localhost:5173"
              )
              .AllowCredentials()  // Required for cookies/sessions
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Content-Disposition", "Content-Type", "Authorization");
    });
});

// Forwarded headers (X-Forwarded-For, X-Forwarded-Proto) so Request.Scheme reflects the original protocol when behind proxies/load balancers
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Clear known networks/proxies so forwarded headers from the platform (Azure) are accepted
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// JWT auth configuration
var jwtKey = builder.Configuration["Jwt:SigningKey"] ?? "dev-only-signing-key-change-me";
var issuer = builder.Configuration["Jwt:Issuer"] ?? "GhanaHybridRental";
var audience = builder.Configuration["Jwt:Audience"] ?? "GhanaHybridRental";
var tokenLifetimeMinutes = int.TryParse(builder.Configuration["Jwt:TokenLifetimeMinutes"], out var tl)
    ? tl : 60;

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = signingKey,
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
    options.AddPolicy("OwnerOnly", policy => policy.RequireRole("owner", "admin")); // Allow both owner and admin
    options.AddPolicy("RenterOnly", policy => policy.RequireRole("renter"));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Ryve Rental API",
        Version = "v1",
        Description = "Complete API for Ryve Rental - Car rental platform with real-time features, insurance options, and third-party integrations",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Ryve Rental API Support",
            Email = "developers@ryverental.com",
            Url = new Uri("https://developers.ryverental.com")
        },
        License = new Microsoft.OpenApi.Models.OpenApiLicense
        {
            Name = "Commercial License",
            Url = new Uri("https://ryverental.com/api-terms")
        }
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Workaround: when duplicate/ambiguous actions (same method+path) are present,
    // Swashbuckle will throw. Resolve duplicates by picking the first action to
    // allow the Swagger document to be generated. Investigate duplicates separately.
    options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
});

var app = builder.Build();

// Respect X-Forwarded headers (must run before generating absolute URLs)
app.UseForwardedHeaders();

// Developer exception page if configured
if (exposeDetailedErrors || app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    // Use a specific error handling path so the middleware can be configured reliably in production
    app.UseExceptionHandler("/error");
} 

// Apply pending migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        if (!db.Database.CanConnect())
        {
            Console.WriteLine("⚠ Could not connect to the database - skipping migrations and optional schema checks");
        }
        else
        {
            db.Database.Migrate();
            Console.WriteLine("✓ Database migrations applied successfully");

            // Helper to ensure optional columns exist (safe, idempotent)
            void EnsureSql(params string[] statements)
            {
                foreach (var s in statements)
                {
                    try
                    {
                        db.Database.ExecuteSqlRaw(s);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠ Could not execute SQL statement: {ex.Message}");
                    }
                }
            }

            EnsureSql(
                // Vehicle availability
                @"ALTER TABLE ""Vehicles"" ADD COLUMN IF NOT EXISTS ""AvailableFrom"" timestamp with time zone NULL;",
                @"ALTER TABLE ""Vehicles"" ADD COLUMN IF NOT EXISTS ""AvailableUntil"" timestamp with time zone NULL;",

                // Vehicle documents
                @"ALTER TABLE ""Vehicles"" ADD COLUMN IF NOT EXISTS ""InsuranceDocumentUrl"" text NULL;",
                @"ALTER TABLE ""Vehicles"" ADD COLUMN IF NOT EXISTS ""RoadworthinessDocumentUrl"" text NULL;",
                @"ALTER TABLE ""Vehicles"" ADD COLUMN IF NOT EXISTS ""DailyRate"" numeric(18,2) NULL;",

                // OwnerProfile columns required by code
                @"ALTER TABLE ""OwnerProfiles"" ADD COLUMN IF NOT EXISTS ""PayoutDetailsPendingJson"" text NULL;",
                @"ALTER TABLE ""OwnerProfiles"" ADD COLUMN IF NOT EXISTS ""PayoutVerificationStatus"" varchar(32) NOT NULL DEFAULT 'unverified';",
                @"ALTER TABLE ""OwnerProfiles"" ADD COLUMN IF NOT EXISTS ""CompanyVerificationStatus"" varchar(32) NOT NULL DEFAULT 'unverified';",
                @"ALTER TABLE ""OwnerProfiles"" ADD COLUMN IF NOT EXISTS ""CompanyNamePending"" varchar(256) NULL;",
                @"ALTER TABLE ""OwnerProfiles"" ADD COLUMN IF NOT EXISTS ""BusinessRegistrationNumberPending"" varchar(128) NULL;",
                @"ALTER TABLE ""OwnerProfiles"" ADD COLUMN IF NOT EXISTS ""Bio"" text NULL;",

                // Ensure Booking.UpdatedAt exists (migration may not have run in some environments)
                @"ALTER TABLE ""Bookings"" ADD COLUMN IF NOT EXISTS ""UpdatedAt"" timestamp without time zone NULL;"
            );

            Console.WriteLine("✓ Ensured optional schema elements (if database permissions allowed it)");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠ Could not apply migrations or ensure optional schema: {ex.Message}");
    }
            try
            {
                // Ensure ProfileChangeAudits table exists (added in a migration that may not have run yet)
                db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS ""ProfileChangeAudits"" (
                    ""Id"" uuid PRIMARY KEY,
                    ""UserId"" uuid NOT NULL,
                    ""Field"" varchar(128) NOT NULL,
                    ""OldValue"" text NULL,
                    ""NewValue"" text NULL,
                    ""ChangedByUserId"" uuid NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT now()
                );");
                Console.WriteLine("✓ Ensured ProfileChangeAudits table exists");

                // Ensure Booking guest columns exist for guest booking flow
                db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Bookings"" ADD COLUMN IF NOT EXISTS ""GuestPhone"" varchar(32) NULL;");
                db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Bookings"" ADD COLUMN IF NOT EXISTS ""GuestEmail"" text NULL;");
                db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Bookings"" ADD COLUMN IF NOT EXISTS ""GuestFirstName"" varchar(128) NULL;");
                db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Bookings"" ADD COLUMN IF NOT EXISTS ""GuestLastName"" varchar(128) NULL;");
                db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Bookings"" ADD COLUMN IF NOT EXISTS ""GuestUserId"" uuid NULL;");
                Console.WriteLine("✓ Ensured Booking guest columns exist");
            }
            catch (Exception ex4)
            {
                Console.WriteLine($"⚠ Could not ensure ProfileChangeAudits table exists: {ex4.Message}");
            }
}


// Enable Swagger in all environments for now
app.UseSwagger();
app.UseSwaggerUI();

// Enable static files for local file uploads
var uploadsPath = builder.Configuration["FileUpload:Path"];
if (string.IsNullOrWhiteSpace(uploadsPath))
{
    uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
}
else if (!Path.IsPathRooted(uploadsPath))
{
    uploadsPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), uploadsPath));
}
try
{
    if (!Directory.Exists(uploadsPath))
    {
        Directory.CreateDirectory(uploadsPath);
    }

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
        RequestPath = "/uploads"
    });
}
catch (Exception ex)
{
    // Log but don't crash if uploads directory can't be created
    app.Logger.LogWarning(ex, "Could not create uploads directory at {Path}. File uploads may not work.", uploadsPath);
}

// Middleware order is critical for CORS to work properly
app.UseCors("AllowAll");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Static files for uploads
app.UseStaticFiles();

// Map endpoints
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapOwnerEndpoints();
app.MapAdminEndpoints();
app.MapInsuranceEndpoints();
app.MapProtectionEndpoints();
app.MapBookingEndpoints();
app.MapDriverEndpoints();
app.MapIntegrationPartnerEndpoints();
app.MapPaymentEndpoints();
app.MapPaymentConfigEndpoints();
app.MapPricingEndpoints();
app.MapRefundPolicyEndpoints();
app.MapReviewEndpoints();
app.MapReportEndpoints();
app.MapInspectionEndpoints();
app.MapRenterEndpoints();
app.MapReferralEndpoints();
app.MapEmailTemplateEndpoints();
app.MapMarketingEmailEndpoints();
app.MapWebhookEndpoints();
app.MapFileUploadEndpoints();
app.MapReceiptEndpoints();
app.MapAccountEndpoints();
app.MapAirportEndpoints();
app.MapRentalAgreementEndpoints();
app.MapChargeEndpoints();
app.MapPartnerEndpoints();
app.MapDepositRefundEndpoints();
app.MapOwnerPayoutEndpoints();
app.MapPromoCodeEndpoints();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}));

// Global error endpoint used by UseExceptionHandler
app.Map("/error", (HttpContext httpContext) =>
{
    var feature = httpContext.Features.Get<IExceptionHandlerFeature>();
    var ex = feature?.Error;
    return Results.Problem(detail: ex?.Message, title: "Internal Server Error");
});

// Log payment provider configuration at startup to help diagnose missing keys
try
{
    using var scope = app.Services.CreateScope();
    var configSvc = scope.ServiceProvider.GetRequiredService<IAppConfigService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var stripeKey = configSvc.GetConfigValueAsync("Payment:Stripe:SecretKey").GetAwaiter().GetResult();
    var paystackKey = configSvc.GetConfigValueAsync("Payment:Paystack:SecretKey").GetAwaiter().GetResult();
    logger.LogInformation("Payment providers configured: Stripe={StripeConfigured}, Paystack={PaystackConfigured}", !string.IsNullOrWhiteSpace(stripeKey), !string.IsNullOrWhiteSpace(paystackKey));
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning(ex, "Failed to read payment provider configuration at startup");
}

app.Run();
