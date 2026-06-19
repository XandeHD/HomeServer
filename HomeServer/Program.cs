using HomeServer;
using HomeServer.Classes.Services;
using HomeServer.Components;
using HomeServer.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google; // Adicionado para suporte ao Google
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContextFactory<DataContext>(options =>
    options.UseSqlite(connectionString));

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddHttpClient();

// CONFIGURAÇÃO DE AUTENTICAÇÃO MISTA (Cookie + Google OAuth)
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30); // Mantém a sessão por 30 dias
    })
    .AddGoogle(googleOptions =>
    {
        // Lembra-te de colocar estas chaves no appsettings.json ou User Secrets em produção!
        googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "O_TEU_CLIENT_ID.apps.googleusercontent.com";
        googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "O_TEU_CLIENT_SECRET";
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<HomeServer.Classes.Services.Theme>();

builder.Services.AddHttpClient<StockService>();

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

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

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

        if (result == PasswordVerificationResult.Success)
        {
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

    return Results.Redirect("/login?error=InvalidCredentials");
});

app.MapPost("/api/auth/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});


// --- NOVOS ENDPOINTS: GOOGLE AUTHENTICATION ---

// Endpoint que inicia o desafio (redireciona para o ecrã da Google)
app.MapGet("/api/auth/login-google", ([FromQuery] string returnUrl = "/") =>
{
    var properties = new AuthenticationProperties
    {
        RedirectUri = $"/api/auth/google-callback?returnUrl={Uri.EscapeDataString(returnUrl)}"
    };
    return Results.Challenge(properties, new[] { GoogleDefaults.AuthenticationScheme });
});

// Endpoint de retorno da Google com Lógica de Registo Automático na BD
app.MapGet("/api/auth/google-callback", async (
    HttpContext httpContext,
    [FromQuery] string returnUrl = "/",
    [FromServices] IDbContextFactory<DataContext> dbFactory = null!) =>
{
    // Lê o resultado da autenticação temporária que a Google enviou
    var result = await httpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

    if (!result.Succeeded || result.Principal == null)
    {
        return Results.Redirect("/login?error=GoogleAuthFailed");
    }

    // Extrai os dados do perfil Google do utilizador
    var email = result.Principal.FindFirstValue(ClaimTypes.Email);
    var name = result.Principal.FindFirstValue(ClaimTypes.Name);

    if (string.IsNullOrEmpty(email))
    {
        return Results.Redirect("/login?error=EmailRequired");
    }

    using var db = await dbFactory.CreateDbContextAsync();

    // 1. Procura na BD se já existe um utilizador com este e-mail
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

    // 2. Se não existir, faz o REGISTO AUTOMÁTICO
    if (user == null)
    {
        user = new User
        {
            Username = name ?? email.Split('@')[0], // Usa o nome da Google ou a primeira parte do email
            Email = email,
            Theme = "theme-default", // Define um tema padrão inicial
            PasswordHash = "OAUTH_GOOGLE_ACCOUNT" // Placeholder para indicar que não usa password local
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    // 3. Procura se este utilizador (existente ou recém-criado) pertence a algum grupo
    var userGroupRel = await db.GroupUsers.FirstOrDefaultAsync(gu => gu.UserId == user.Id);

    // 4. Monta a identidade final com base nos dados da tua base de dados (mantém a consistência da app)
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

    // 5. Inicia a sessão definitiva gravando o Cookie padrão da tua aplicação
    await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

    return Results.Redirect(returnUrl);
});


// --- AUTOMATIC DATABASE SEEDER BLOCK ---
using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DataContext>>();
    using var db = await dbFactory.CreateDbContextAsync();

    if (!await db.Users.AnyAsync())
    {
        var hasher = new PasswordHasher<User>();

        var adminUser = new User { Username = "Alexandre", Email = "gdmfplays@gmail.com" };
        adminUser.PasswordHash = hasher.HashPassword(adminUser, "mypassword");

        var wifeUser = new User { Username = "Tays", Email = "gdmfplays@gmail.com" };
        wifeUser.PasswordHash = hasher.HashPassword(wifeUser, "amordaminhavida123");

        db.Users.AddRange(adminUser, wifeUser);
        await db.SaveChangesAsync();
    }
}

app.Run();