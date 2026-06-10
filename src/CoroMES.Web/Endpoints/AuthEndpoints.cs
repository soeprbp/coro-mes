using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace CoroMES.Web.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/login", (HttpContext context, IWebHostEnvironment environment, IConfiguration configuration) =>
        {
            var returnUrl = GetSafeReturnUrl(context.Request.Query["returnUrl"].ToString());
            var error = context.Request.Query.ContainsKey("error");
            var devHint = environment.IsDevelopment() && string.IsNullOrWhiteSpace(configuration["Auth:AdminAccessCode"])
                ? "<p class=\"hint\">Development access code: <code>dev-admin</code></p>"
                : string.Empty;
            var errorHtml = error ? "<p class=\"error\">Access code was not accepted.</p>" : string.Empty;
            var encodedReturnUrl = HtmlEncoder.Default.Encode(returnUrl);

            return Results.Content($$"""
                <!doctype html>
                <html lang="en">
                <head>
                    <meta charset="utf-8" />
                    <meta name="viewport" content="width=device-width, initial-scale=1" />
                    <title>CoroMES Admin Sign In</title>
                    <style>
                        body { margin: 0; min-height: 100vh; display: grid; place-items: center; background: #f5f7fa; color: #17202a; font-family: Segoe UI, Arial, sans-serif; }
                        main { width: min(420px, calc(100vw - 32px)); padding: 28px; border: 1px solid #d9e0e8; border-radius: 8px; background: #fff; }
                        h1 { margin: 0 0 8px; font-size: 1.45rem; }
                        p { color: #607080; }
                        label { display: grid; gap: 8px; margin-top: 18px; color: #607080; font-size: .9rem; }
                        input { padding: 10px 12px; border: 1px solid #d9e0e8; border-radius: 6px; font: inherit; }
                        button { width: 100%; margin-top: 18px; padding: 10px 12px; border: 1px solid #176b87; border-radius: 6px; background: #176b87; color: #fff; font: inherit; cursor: pointer; }
                        .error { color: #b42318; font-weight: 700; }
                        .hint code { color: #17202a; }
                    </style>
                </head>
                <body>
                    <main>
                        <h1>CoroMES Admin</h1>
                        <p>Sign in to manage equipment, integrations, and MES records.</p>
                        {{errorHtml}}
                        {{devHint}}
                        <form method="post" action="/login">
                            <input type="hidden" name="returnUrl" value="{{encodedReturnUrl}}" />
                            <label>Access code <input name="accessCode" type="password" autocomplete="current-password" required autofocus /></label>
                            <button type="submit">Sign In</button>
                        </form>
                    </main>
                </body>
                </html>
                """, "text/html");
        }).AllowAnonymous();

        app.MapPost("/login", async (HttpContext context, IWebHostEnvironment environment, IConfiguration configuration) =>
        {
            var form = await context.Request.ReadFormAsync();
            var returnUrl = GetSafeReturnUrl(form["returnUrl"].ToString());
            var accessCode = form["accessCode"].ToString();
            var configuredAccessCode = configuration["Auth:AdminAccessCode"];
            var expectedAccessCode = !string.IsNullOrWhiteSpace(configuredAccessCode)
                ? configuredAccessCode
                : environment.IsDevelopment() ? "dev-admin" : null;

            if (string.IsNullOrWhiteSpace(expectedAccessCode) || !TimeSafeEquals(accessCode, expectedAccessCode))
            {
                return Results.Redirect($"/login?error=1&returnUrl={Uri.EscapeDataString(returnUrl)}");
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "CoroMES Admin"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            return Results.Redirect(returnUrl);
        }).AllowAnonymous();

        app.MapGet("/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/login");
        });

        app.MapGet("/access-denied", () => Results.Content("""
            <!doctype html>
            <html lang="en">
            <head><meta charset="utf-8" /><meta name="viewport" content="width=device-width, initial-scale=1" /><title>Access Denied</title></head>
            <body style="font-family: Segoe UI, Arial, sans-serif; margin: 40px;">
                <h1>Access denied</h1>
                <p>Your account does not have permission to view this CoroMES area.</p>
                <a href="/login">Sign in again</a>
            </body>
            </html>
            """, "text/html")).AllowAnonymous();

        return app;
    }

    public static bool RequiresAdminGate(PathString path)
    {
        return path.StartsWithSegments("/admin") ||
            string.Equals(path.Value, "/displays", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/displays/builder");
    }

    private static string GetSafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith("/", StringComparison.Ordinal) || returnUrl.StartsWith("//", StringComparison.Ordinal))
        {
            return "/admin";
        }

        return returnUrl;
    }

    private static bool TimeSafeEquals(string candidate, string expected)
    {
        var candidateBytes = System.Text.Encoding.UTF8.GetBytes(candidate);
        var expectedBytes = System.Text.Encoding.UTF8.GetBytes(expected);
        if (candidateBytes.Length != expectedBytes.Length)
        {
            return false;
        }

        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(candidateBytes, expectedBytes);
    }
}
