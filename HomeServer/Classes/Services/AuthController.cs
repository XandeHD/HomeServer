using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    [HttpGet("login-google")]
    public IActionResult LoginGoogle([FromQuery] string returnUrl = "/")
    {
        // Configura para onde o utilizador deve ir após o login com sucesso na Google
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCallback), new { returnUrl })
        };

        // Dispara o ecrã da Google
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google-callback")]
    public async Task<IActionResult> GoogleCallback(string returnUrl = "/")
    {
        // Aqui o utilizador já voltou autenticado da Google!
        var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!authenticateResult.Succeeded)
            return RedirectToPage("/login"); // Falhou o login

        // Podes ler os dados que a Google te deu (Email, Nome, ID único)
        var email = authenticateResult.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var name = authenticateResult.Principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

        // ====================================================================
        // LÓGICA DE REGISTO AUTOMÁTICO (O TEU DESAFIO)
        // ====================================================================
        // usando var db = ... (injetar o teu IDbContextFactory<DataContext>)
        // 1. Procurar na BD se já existe um utilizador com este Email.
        // 2. Se NÃO existir:
        //    - Cria um registo novo na tabela Users com este email, name, e tema default.
        // 3. Se JÁ existir:
        //    - Segue em frente (já está registado).
        // ====================================================================

        return LocalRedirect(returnUrl);
    }
}