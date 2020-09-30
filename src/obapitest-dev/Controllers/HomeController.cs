using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using obapitest_dev.Models;
using Seges.Extensions.Identity.Core.DeflatedSaml;
using Seges.Extensions.Identity.Core.WsTrust;

namespace obapitest_dev.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> ObapiViaHybridConnection(string scheme = "https")
        {
            var obapiDomain = "devtest-obapi.vfltest.dk";
            var adfsDomain = "devtest-idp.vfltest.dk";
            WsTrustClient c = new WsTrustClient(adfsDomain);
            var tokenRequest = new SamlTokenRequest
            {
                Audience = $"https://{obapiDomain}/",
                Username = _configuration["username"],
                Password = _configuration["password"],
            };
            var tokenResponse = await c.RequestTokenAsync(tokenRequest);
            var encodedToken = new DeflatedSamlEncoder().Encode(tokenResponse.TokenXml);
            using var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, $"{scheme}://{obapiDomain}/v2/Users/");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", encodedToken);
            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            var output =
                $"User: {tokenRequest.Username}{Environment.NewLine}Request url: {request.RequestUri}{Environment.NewLine}StatusCode: {response.StatusCode}{Environment.NewLine}Content:{Environment.NewLine}{responseContent}";
            return this.Content(output);
        }



        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
