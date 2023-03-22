using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Sales.WEB.Auth
{
    public class AuthenticationProviderTest : AuthenticationStateProvider
    {
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            await Task.Delay(3000);
            //ClaimsIdentity > reclamaciones
            var anonimous = new ClaimsIdentity();
            var zulu = new ClaimsIdentity(new List<Claim>
        {
            new Claim("FirstName", "Juan"),
            new Claim("LastName", "Zulu"),
            new Claim(ClaimTypes.Name, "zulu@yopmail.com"),
            new Claim(ClaimTypes.Role, "Admin")
        },
         authenticationType: "test");
            return await Task.FromResult(new AuthenticationState(new ClaimsPrincipal(zulu)));
        }
    }
}
