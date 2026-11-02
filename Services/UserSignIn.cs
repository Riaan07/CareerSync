using CareerSync.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace CareerSync.Services
{
    /// <summary>
    /// Issues the sign-in cookie. Shared so that when a user edits their name
    /// on a Profile page, the cookie (which is where the sidebar reads the name
    /// from) can be re-issued immediately instead of showing the old name
    /// until they next log in.
    /// </summary>
    public static class UserSignIn
    {
        public static Task SignInAsync(HttpContext http, AppUser user, bool persistent)
        {
            var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
        };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            return http.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = persistent, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14) });
        }

        /// <summary>Re-issue the cookie with fresh details, keeping the user's
        /// original "Keep me signed in" choice.</summary>
        public static async Task RefreshAsync(HttpContext http, AppUser user)
        {
            var current = await http.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await SignInAsync(http, user, current.Properties?.IsPersistent ?? false);
        }
    }

}
