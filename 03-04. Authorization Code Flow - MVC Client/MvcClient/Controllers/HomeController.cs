using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using IdentityModel.Client;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

using MvcClient.Models;
using System.Net;

namespace MvcClient.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> PrivacyAsync()
        {
            var client = new HttpClient();
            var discovery = await client.GetDiscoveryDocumentAsync("https://localhost:5001");
            if (discovery.IsError)
            {
                throw new Exception(discovery.Error);
            }

            var accessToken = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);

            client.SetBearerToken(accessToken);

            var response = await client.GetAsync("https://localhost:6001/identity");
            if (!response.IsSuccessStatusCode)
            {
                if(response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await RenewTokenAsync();
                    return RedirectToAction();
                }
                else
                {
                    throw new Exception(response.ReasonPhrase);
                }
            }

            var content = await response.Content.ReadAsStringAsync();

            return View("Privacy", content);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult Logout()
        {
            return SignOut(CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        private async Task<string> RenewTokenAsync()
        {
            var client = new HttpClient();
            var discovery = await client.GetDiscoveryDocumentAsync("https://localhost:5001");
            if (discovery.IsError)
            {
                throw new Exception(discovery.Error);
            }

            var refreshToken = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.RefreshToken);
            var tokenResponse = await client.RequestRefreshTokenAsync(new RefreshTokenRequest()
            {
                Address = discovery.TokenEndpoint,
                ClientId = "mvc",
                ClientSecret = "secret",
                Scope = "api1 openid profile email phone adress offline_access",
                GrantType = OpenIdConnectGrantTypes.RefreshToken,
                RefreshToken = refreshToken,
            });

            if (tokenResponse.IsError)
            {
                throw new Exception(tokenResponse.Error);
            }
            else
            {
                var expiresAt = DateTime.UtcNow + TimeSpan.FromSeconds(tokenResponse.ExpiresIn);

                var tokens = new[]
                {
                    new AuthenticationToken
                    {
                        Name = OpenIdConnectParameterNames.IdToken,
                        Value = tokenResponse.IdentityToken,
                    },
                    new AuthenticationToken
                    {
                        Name = OpenIdConnectParameterNames.AccessToken,
                        Value = tokenResponse.AccessToken,
                    },
                    new AuthenticationToken
                    {
                        Name = OpenIdConnectParameterNames.RefreshToken,
                        Value = tokenResponse.RefreshToken,
                    },
                    new AuthenticationToken
                    {
                        Name = "expires_at",
                        Value = expiresAt.ToString("o",CultureInfo.InvariantCulture),
                    }
                };

                //获取身份认证的结果，包含当前的principal和properties
                var currentAuthenticationResult =
                    await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                //把新的tokens存起来
                currentAuthenticationResult.Properties.StoreTokens(tokens);

                //重新登录
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    currentAuthenticationResult.Principal,
                    currentAuthenticationResult.Properties);

                return tokenResponse.AccessToken;
            }
        }
    }
}
