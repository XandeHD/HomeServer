using HomeServer;
using HomeServer.Classes.Services;
using HomeServer.Components;
using HomeServer.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContextFactory<DataContext>(options =>
    options.UseSqlite(connectionString));

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddHttpClient();

// --- RATE LIMITING: protege o endpoint de login contra ataques de força bruta ---
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(5);
        limiterOptions.PermitLimit = 10;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
});

// CONFIGURAÇÃO DE AUTENTICAÇÃO MISTA (Cookie + Google OAuth)
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
var googleConfigured = !string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret);

var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

if (googleConfigured)
{
    authBuilder.AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = googleClientId!;
        googleOptions.ClientSecret = googleClientSecret!;
    });
}

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<HomeServer.Classes.Services.Theme>();
builder.Services.AddHttpClient<StockService>();

// Circuito Blazor Server: limitar conexões concorrentes por IP para evitar flooding
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 32 * 1024; // 32KB max message
});

// Localização (i18n)
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddScoped<HomeServer.Classes.Services.LocalizationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// Cabeçalhos de segurança HTTP
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// --- ENDPOINTS DE AUTENTICAÇÃO TRADICIONAL ---

app.MapPost("/api/auth/login", async (
    HttpContext httpContext,
    [FromForm] string username,
    [FromForm] string password,
    [FromServices] IDbContextFactory<DataContext> dbFactory) =>
{
    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
    {
        return Results.Redirect("/login?error=InvalidCredentials");
    }

    using var db = await dbFactory.CreateDbContextAsync();
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

    if (user != null)
    {
        var hasher = new PasswordHasher<User>();
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, password);

        if (result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = hasher.HashPassword(user, password);
                await db.SaveChangesAsync();
            }

            var userGroupRel = await db.GroupUsers.FirstOrDefaultAsync(gu => gu.UserId == user.Id);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            if (userGroupRel != null)
            {
                claims.Add(new Claim(ClaimTypes.GroupSid, userGroupRel.GroupId.ToString()));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            return Results.Redirect("/");
        }
    }

    await Task.Delay(Random.Shared.Next(200, 500));
    return Results.Redirect("/login?error=InvalidCredentials");
}).RequireRateLimiting("login");

app.MapPost("/api/auth/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

// --- ENDPOINTS GOOGLE AUTHENTICATION ---

app.MapGet("/api/auth/login-google", ([FromQuery] string returnUrl = "/") =>
{
    if (!googleConfigured)
        return Results.Redirect("/login?error=GoogleNotConfigured");

    if (!Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
        returnUrl = "/";

    var properties = new AuthenticationProperties
    {
        RedirectUri = $"/api/auth/google-callback?returnUrl={Uri.EscapeDataString(returnUrl)}"
    };
    return Results.Challenge(properties, new[] { GoogleDefaults.AuthenticationScheme });
});

app.MapGet("/api/auth/google-callback", async (
    HttpContext httpContext,
    [FromQuery] string returnUrl = "/",
    [FromServices] IDbContextFactory<DataContext> dbFactory = null!) =>
{
    if (!Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
    {
        returnUrl = "/";
    }

    var result = await httpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

    if (!result.Succeeded || result.Principal == null)
    {
        return Results.Redirect("/login?error=GoogleAuthFailed");
    }

    var email = result.Principal.FindFirstValue(ClaimTypes.Email);
    var name = result.Principal.FindFirstValue(ClaimTypes.Name);

    if (string.IsNullOrEmpty(email))
    {
        return Results.Redirect("/login?error=EmailRequired");
    }

    using var db = await dbFactory.CreateDbContextAsync();

    var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

    if (user == null)
    {
        user = new User
        {
            Username = name ?? email.Split('@')[0],
            Email = email,
            Theme = "default",
            PasswordHash = "OAUTH_GOOGLE_ACCOUNT"
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    var userGroupRel = await db.GroupUsers.FirstOrDefaultAsync(gu => gu.UserId == user.Id);

    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
    };

    if (userGroupRel != null)
    {
        claims.Add(new Claim(ClaimTypes.GroupSid, userGroupRel.GroupId.ToString()));
    }

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

    return Results.Redirect(returnUrl);
});


// --- AUTOMATIC DATABASE SEEDER ---
using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DataContext>>();
    using var db = await dbFactory.CreateDbContextAsync();

    // Cria o schema completo se a BD não existir; não-destrutivo em BDs existentes
    await db.Database.EnsureCreatedAsync();

    // Adiciona coluna Language à tabela users se não existir (upgrade de BDs antigas)
    var conn = db.Database.GetDbConnection();
    if (conn.State != System.Data.ConnectionState.Open)
        await conn.OpenAsync();

    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = "SELECT COUNT(*) FROM pragma_table_info('users') WHERE name='Language'";
        var count = (long)(await cmd.ExecuteScalarAsync() ?? 0L);
        if (count == 0)
        {
            cmd.CommandText = "ALTER TABLE users ADD COLUMN \"Language\" TEXT NOT NULL DEFAULT 'pt'";
            await cmd.ExecuteNonQueryAsync();
        }
    }

    if (!await db.Users.AnyAsync())
    {
        var hasher = new PasswordHasher<User>();

        var adminPassword = builder.Configuration["Seed:AdminPassword"] ?? "ChangeMe123!";
        var secondPassword = builder.Configuration["Seed:SecondUserPassword"] ?? "ChangeMe123!";

        var adminUser = new User { Username = "Alexandre", Email = "admin@homeserver.local", Theme = "default" };
        adminUser.PasswordHash = hasher.HashPassword(adminUser, adminPassword);

        var secondUser = new User { Username = "Tays", Email = "tays@homeserver.local", Theme = "default" };
        secondUser.PasswordHash = hasher.HashPassword(secondUser, secondPassword);

        db.Users.AddRange(adminUser, secondUser);
        await db.SaveChangesAsync();
    }
}

app.Run();
