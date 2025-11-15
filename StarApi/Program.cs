using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using StarApi.Data;
using StarApi.Filters;
using StarApi.Middleware;
using StarApi.Services;

// Load .env file if it exists
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "API Key needed to access the endpoints. X-API-Key: your-api-key",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "X-API-Key",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });

    // Add file upload support for Swagger
    c.OperationFilter<FileUploadOperationFilter>();

    // Map IFormFile to file upload in Swagger
    c.MapType<IFormFile>(() => new Microsoft.OpenApi.Models.OpenApiSchema
    {
        Type = "string",
        Format = "binary"
    });
});

// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add CORS - Configure allowed origins for production
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        // TODO: Replace with your actual frontend domain(s)
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
            ?? new[] {  "http://star.vividcats.org","http://localhost:3000", "http://localhost:5173" }; // Default for development

        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add Authentication: Support both JWT Bearer and existing API Key via a policy scheme
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = "MultiAuth";
        options.DefaultChallengeScheme = "MultiAuth";
    })
    .AddPolicyScheme("MultiAuth", "JWT or API Key", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var authHeader = context.Request.Headers["Authorization"].ToString();
            var hasApiKey = context.Request.Headers.ContainsKey("X-API-Key");
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
            }
            if (hasApiKey)
            {
                return "ApiKey";
            }
            // Fallback to JWT so anonymous endpoints still work without failing this stage
            return Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
        };
    })
    .AddJwtBearer(options =>
    {
        var jwtSection = builder.Configuration.GetSection("Jwt");
        var issuer = jwtSection.GetValue<string>("Issuer") ?? "StarApi";
        var audience = jwtSection.GetValue<string>("Audience") ?? "StarApiAudience";
        var key = jwtSection.GetValue<string>("Key");
        if (string.IsNullOrEmpty(key))
        {
            throw new InvalidOperationException("JWT Key is not configured. Set Jwt:Key in environment variables or user secrets.");
        }
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key))
        };
    })
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", options => { });

builder.Services.AddAuthorization();

// Add rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
});

// Configure DigitalOcean Spaces options and service using AWS configuration
// Support multiple configuration sources with fallbacks (like memeService)
builder.Services.Configure<SpacesOptions>(options =>
{
    var awsSection = builder.Configuration.GetSection("AWS");

    options.AccessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY")
                       ?? Environment.GetEnvironmentVariable("AWS__AccessKey")
                       ?? awsSection.GetValue<string>("AccessKey")
                       ?? "";

    options.SecretKey = Environment.GetEnvironmentVariable("AWS_SECRET_KEY")
                       ?? Environment.GetEnvironmentVariable("AWS__SecretKey")
                       ?? awsSection.GetValue<string>("SecretKey")
                       ?? "";

    options.Endpoint = Environment.GetEnvironmentVariable("AWS_SERVICE_URL")
                      ?? Environment.GetEnvironmentVariable("AWS__ServiceURL")
                      ?? awsSection.GetValue<string>("ServiceURL")
                      ?? "";

    options.Bucket = Environment.GetEnvironmentVariable("AWS_BUCKET_NAME")
                    ?? Environment.GetEnvironmentVariable("AWS__BucketName")
                    ?? awsSection.GetValue<string>("BucketName")
                    ?? "";

    options.CdnBaseUrl = Environment.GetEnvironmentVariable("AWS_CDN_BASE_URL")
                        ?? Environment.GetEnvironmentVariable("AWS__CdnBaseUrl")
                        ?? awsSection.GetValue<string>("CdnBaseUrl")
                        ?? "";

    options.UseHttp = Environment.GetEnvironmentVariable("AWS_USE_HTTP") == "true"
                     || Environment.GetEnvironmentVariable("AWS__UseHttp") == "true"
                     || awsSection.GetValue<bool>("UseHttp");
});

builder.Services.AddSingleton<IDigitalOceanSpacesService, DigitalOceanSpacesService>();

var app = builder.Build();

// Run migrations automatically
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Enable HSTS in production
    app.UseHsts();
}

// Security Headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "no-referrer");
    context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'");
    await next();
});

// Enforce HTTPS
app.UseHttpsRedirection();

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();