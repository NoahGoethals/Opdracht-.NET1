using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Data.Seed;
using WorkoutCoachV2.Model.Models;
using WorkoutCoachV2.Web.Middleware;

var builder = WebApplication.CreateBuilder(args); // Builder: config + DI + logging + hosting

var connectionString =
    builder.Configuration.GetConnectionString("WorkoutCoachConnection") // Lees connectionstring uit config
    ?? throw new InvalidOperationException("Connection string 'WorkoutCoachConnection' not found."); // Fail fast als hij ontbreekt

builder.Services.AddDbContext<AppDbContext>(options => // Registreer DbContext in DI
    options.UseSqlServer(connectionString, sql => // Configureer SQL Server provider
    {
        sql.EnableRetryOnFailure( // Automatisch retry bij transient SQL errors
            maxRetryCount: 8, // Max 8 retries
            maxRetryDelay: TimeSpan.FromSeconds(10), // Max 10s delay tussen retries
            errorNumbersToAdd: null); // Geen extra error codes toegevoegd

        sql.CommandTimeout(60); // SQL command timeout = 60s
    }));

builder.Services.AddHttpContextAccessor(); // IHttpContextAccessor beschikbaar maken


builder.Services.AddLocalization(options => options.ResourcesPath = "Resources"); // .resx resources zitten in /Resources

builder.Services
    .AddControllersWithViews() // MVC controllers + views
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix) // Views kunnen suffix per taal hebben (bv Index.nl.cshtml)
    .AddDataAnnotationsLocalization(); // Vertalingen voor DataAnnotations (Required, Display, ...)

builder.Services.Configure<RequestLocalizationOptions>(options => // Configureer taal/cultuur per request
{
    var supportedCultures = new[] // Culturen die je app ondersteunt
    {
        new CultureInfo("en"),
        new CultureInfo("nl"),
        new CultureInfo("fr")
    };

    options.DefaultRequestCulture = new RequestCulture("nl"); // Standaard taal = NL
    options.SupportedCultures = supportedCultures; // Formatting cultuur (datum/getallen)
    options.SupportedUICultures = supportedCultures; // UI cultuur (vertalingen)

    options.RequestCultureProviders = new IRequestCultureProvider[] // Hoe taal gekozen wordt
    {
        new CookieRequestCultureProvider() // Alleen via cookie (niet via header/querystring)
    };
});


builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options => // Identity configuratie (users + roles)
    {
        options.SignIn.RequireConfirmedAccount = false; // Geen email-confirmatie nodig

        options.Password.RequireNonAlphanumeric = false; // Speciale tekens niet verplicht
        options.Password.RequireUppercase = false; // Hoofdletters niet verplicht
        options.Password.RequiredLength = 6; // Minimum lengte = 6
    })
    .AddEntityFrameworkStores<AppDbContext>() // Identity bewaart data in AppDbContext
    .AddDefaultTokenProviders(); // Tokens voor reset/confirm/etc.

builder.Services.ConfigureApplicationCookie(options => // Cookie settings (voor MVC/web)
{
    options.LoginPath = "/Account/Login"; // Redirect bij niet ingelogd
    options.LogoutPath = "/Account/Logout"; // Logout endpoint
    options.AccessDeniedPath = "/Account/AccessDenied"; // Redirect bij verboden toegang

    options.Events = new CookieAuthenticationEvents // Override redirect gedrag
    {
        OnRedirectToLogin = ctx => // Wanneer cookie auth naar login zou redirecten
        {
            if (ctx.Request.Path.StartsWithSegments("/api")) // Voor API calls: geen HTML redirect
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; // 401 teruggeven
                return Task.CompletedTask;
            }

            ctx.Response.Redirect(ctx.RedirectUri); // Voor web: normale redirect naar login
            return Task.CompletedTask;
        },
        OnRedirectToAccessDenied = ctx => // Wanneer cookie auth naar access denied zou redirecten
        {
            if (ctx.Request.Path.StartsWithSegments("/api")) // Voor API calls: geen HTML redirect
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden; // 403 teruggeven
                return Task.CompletedTask;
            }

            ctx.Response.Redirect(ctx.RedirectUri); // Voor web: normale redirect
            return Task.CompletedTask;
        }
    };
});


builder.Services.AddAuthentication(options => // Auth configuratie
{
    options.DefaultScheme = "Smart"; // Default scheme = policy scheme
    options.DefaultAuthenticateScheme = "Smart"; // Default authenticate scheme = Smart
    options.DefaultChallengeScheme = "Smart"; // Default challenge scheme = Smart
})
.AddPolicyScheme("Smart", "Smart scheme", options => // Policy scheme kiest automatisch cookie of JWT
{
    options.ForwardDefaultSelector = context => // Bepaalt welke scheme gebruikt wordt
    {
        if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)) // API pad?
            return JwtBearerDefaults.AuthenticationScheme; // -> JWT Bearer

        return IdentityConstants.ApplicationScheme; // Anders -> Identity cookie scheme
    };
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options => // JWT Bearer voor /api
{
    var key =
        builder.Configuration["Jwt:Key"] // Key uit config (hierarchisch)
        ?? builder.Configuration["Jwt__Key"] // Key uit Azure App Settings style (double underscore)
        ?? (builder.Environment.IsDevelopment()
            ? "DEV_ONLY_CHANGE_ME_32_CHARS_MINIMUM_KEY!!" // Dev fallback key
            : throw new InvalidOperationException("Jwt key ontbreekt. Zet Azure App Setting 'Jwt__Key' (32+ tekens).")); // Prod: verplicht

    var issuer =
        builder.Configuration["Jwt:Issuer"] // Issuer uit config
        ?? builder.Configuration["Jwt__Issuer"] // Azure variant
        ?? "WorkoutCoachV2"; // Default issuer

    var audience =
        builder.Configuration["Jwt:Audience"] // Audience uit config
        ?? builder.Configuration["Jwt__Audience"] // Azure variant
        ?? "WorkoutCoachV2.Maui"; // Default audience (MAUI client)

    options.TokenValidationParameters = new TokenValidationParameters // Regels om token te valideren
    {
        ValidateIssuer = true, // Issuer claim moet kloppen
        ValidateAudience = true, // Audience claim moet kloppen
        ValidateIssuerSigningKey = true, // Signing key moet kloppen
        ValidateLifetime = true, // Token mag niet verlopen zijn

        ValidIssuer = issuer, // Verwachte issuer
        ValidAudience = audience, // Verwachte audience
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), // Key bytes (HMAC)
        ClockSkew = TimeSpan.FromMinutes(2) // 2 min tijdsmarge
    };
});


builder.Services.AddAuthorization(options => // Autorisatie policies
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin", "Moderator")); // Policy: Admin OF Moderator
});

if (builder.Environment.IsDevelopment()) // Alleen in Development
{
    if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS"))) // Als URLs niet extern gezet zijn
    {
        builder.WebHost.UseUrls("http://localhost:5162", "https://localhost:7289"); // Default lokale luister-URLs
    }
}

var app = builder.Build(); // Bouw de app (middleware pipeline + endpoints)


app.UseForwardedHeaders(new ForwardedHeadersOptions // Forwarded headers (reverse proxy support)
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto // IP + protocol overnemen
});


var locOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>(); // Localization options uit DI
app.UseRequestLocalization(locOptions.Value); // Pas localization toe per request


try
{
    await DbSeeder.SeedAsync(app.Services); // Seed/migrate DB bij startup
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup"); // Logger maken
    logger.LogError(ex, "Database seeding/migration failed at startup."); // Error loggen
    throw; // App laten falen (zodat je probleem ziet)
}


if (!app.Environment.IsDevelopment()) // Buiten development
{
    app.UseExceptionHandler("/Home/Error"); // Custom error page
    app.UseHsts(); // HSTS (dwingt HTTPS in browsers)
}

app.UseHttpsRedirection(); // HTTP -> HTTPS
app.UseStaticFiles(); // wwwroot static files

app.UseRouting(); // Routing aanzetten

app.UseAuthentication(); // Auth uitvoeren (cookie of JWT via Smart scheme)
app.UseMiddleware<BlockedUserMiddleware>(); // Custom middleware: blokkeert geblokkeerde users
app.UseAuthorization(); // Autorisatie checks

app.MapControllers(); // Attribute-routed controllers

app.MapControllerRoute( // Default MVC route
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


try
{
    var addresses = app.Services.GetRequiredService<IServerAddressesFeature>()?.Addresses; // Server listening addresses ophalen
    if (addresses is not null) // Als we ze hebben
    {
        Console.WriteLine("🌐 Listening on:"); // Print header
        foreach (var a in addresses) Console.WriteLine("   " + a); // Print elke URL
    }
}
catch
{
    // Eventuele fouten negeren (logging mag app niet crashen)
}

app.Run(); // Start Kestrel en begin requests te verwerken
