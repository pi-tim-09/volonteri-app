using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebApp.Data;
using WebApp.Interfaces;
using WebApp.Interfaces.Services;
using WebApp.Middleware;
using WebApp.Patterns.Behavioral;
using WebApp.Patterns.Creational;
using WebApp.Patterns.Structural;
using WebApp.Repositories;
using WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Database based on environment
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (builder.Environment.IsProduction())
{
    // Production: Use PostgreSQL from environment variable (Render)
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    
    if (!string.IsNullOrEmpty(databaseUrl))
    {
        // Parse Render's DATABASE_URL format: postgresql://user:password@host:port/database
        var databaseUri = new Uri(databaseUrl);
        var userInfo = databaseUri.UserInfo.Split(':');
        
        connectionString = $"Host={databaseUri.Host};Port={databaseUri.Port};Database={databaseUri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
    }
    
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    // Development: Use SQL Server locally
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
}

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<IUserFactory, VolunteerFactory>();
builder.Services.AddScoped<IUserFactory, OrganizationFactory>();
builder.Services.AddScoped<IUserFactory, AdminFactory>();
builder.Services.AddScoped<IUserFactoryProvider, UserFactoryProvider>();


builder.Services.AddScoped<INotificationService>(serviceProvider =>
{
    var baseService = new BaseNotificationService();
    var loggingDecorator = new LoggingNotificationDecorator(
        baseService,
        serviceProvider.GetRequiredService<ILogger<LoggingNotificationDecorator>>());
    var emailDecorator = new EmailNotificationDecorator(
        loggingDecorator,
        serviceProvider.GetRequiredService<ILogger<EmailNotificationDecorator>>());
    var statisticsDecorator = new StatisticsNotificationDecorator(
        emailDecorator,
        serviceProvider.GetRequiredService<ILogger<StatisticsNotificationDecorator>>());
    return statisticsDecorator;
});

// 3. STATE PATTERN (Behavioral)
builder.Services.AddScoped<IApplicationStateFactory, ApplicationStateFactory>();
builder.Services.AddScoped<IApplicationStateContextFactory, ApplicationStateContextFactory>();

// ============================================================================
// VOLUNTEER-SPECIFIC DESIGN PATTERNS (for volunteer module only)
// ============================================================================

// 4. VOLUNTEER PROFILE DECORATOR PATTERN (Structural)
builder.Services.AddScoped<IVolunteerProfileService>(serviceProvider =>
{
    var baseService = new BasicVolunteerProfileService();
    var validatingDecorator = new ValidatingVolunteerProfileDecorator(
        baseService,
        serviceProvider.GetRequiredService<ILogger<ValidatingVolunteerProfileDecorator>>());
    var loggingDecorator = new LoggingVolunteerProfileDecorator(
        validatingDecorator,
        serviceProvider.GetRequiredService<ILogger<LoggingVolunteerProfileDecorator>>());
    var enrichedDecorator = new EnrichedVolunteerProfileDecorator(
        loggingDecorator,
        serviceProvider.GetRequiredService<ILogger<EnrichedVolunteerProfileDecorator>>());
    return enrichedDecorator;
});

// 5. VOLUNTEER OBSERVER PATTERN (Behavioral)
builder.Services.AddSingleton<IVolunteerEventPublisher, VolunteerEventPublisher>();
builder.Services.AddSingleton<IVolunteerObserver, LoggingVolunteerObserver>();
builder.Services.AddSingleton<IVolunteerObserver, StatisticsVolunteerObserver>();
builder.Services.AddSingleton<IVolunteerObserver, NotificationVolunteerObserver>();


builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasherService>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession
    ();

// Configure JWT Key - use environment variable in production
var jwtKey = builder.Configuration["JWT:SecureKey"];

if (builder.Environment.IsProduction())
{
    // In production, use environment variable
    jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? jwtKey;
}

if (string.IsNullOrEmpty(jwtKey))
{
    throw new KeyNotFoundException("JWT Secure Key not found in configuration or environment variables.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };
    });


builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });
builder.Services.AddAuthentication("MyCookieAuth")
    .AddCookie("MyCookieAuth", options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Volunteer Management API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Upiši: Bearer {JWT token}"
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
            new List<string>()
        }
    });
});


var secureCookiePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = secureCookiePolicy;
});

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.Secure = secureCookiePolicy;
});

var app = builder.Build();
app.UseGlobalExceptionHandler();

app.UseSecurityHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Volunteer API v1");
    options.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseCookiePolicy();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program { }