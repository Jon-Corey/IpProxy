using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace IpProxy
{
    public class IpProxy
    {
        private readonly ILogger<IpProxy> _logger;

        public IpProxy(ILogger<IpProxy> logger)
        {
            _logger = logger;
        }

        [Function("IpProxy")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            _logger.LogInformation("IpProxy Function recieved a request.");

            _ = bool.TryParse(req.Query["keepalive"].ToString(), out bool isKeepAlive);
            if (isKeepAlive)
            {
                _logger.LogInformation("Request is a Keep Alive request. Ignoring.");
                return new NoContentResult();
            }

            string clientIp = req.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            if (req.Headers.TryGetValue("X-Forwarded-For", out Microsoft.Extensions.Primitives.StringValues value))
            {
                clientIp = value.ToString().Split(',')[0];
            }

#if DEBUG
            // Override IP address for testing locally
            clientIp = "203.0.113.20";
#endif

            if (IPAddress.TryParse(clientIp, out var ip))
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(3);

                try
                {
                    var response = await client.GetAsync($"http://ip-api.com/json/{ip}?fields=33615865");

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation($"Successfully processed IP: {ip}.");

                        var result = await response.Content.ReadFromJsonAsync<IpApiResult>();

                        if (result?.Status == "success")
                        {
                            return new OkObjectResult(result);
                        }
                        else
                        {
                            return new BadRequestObjectResult(result);
                        }
                    }
                    else
                    {
                        var body = await response.Content.ReadAsStringAsync();
                        var result = new ObjectResult(body);

                        if (int.TryParse(response.StatusCode.ToString(), out int statusCode))
                        {
                            result.StatusCode = statusCode;
                        }
                        else
                        {
                            result.StatusCode = StatusCodes.Status500InternalServerError;
                        }

                        _logger.LogInformation($"Request to API was not a success. Code: {response.StatusCode}; Message: {body};");
                        return result;
                    }
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                {
                    _logger.LogInformation($"Request to API timed out: {clientIp}.");
                    var result = new ObjectResult("Request to IP-API.com timed out.");
                    result.StatusCode = StatusCodes.Status504GatewayTimeout;
                    return result;
                }
            }
            else
            {
                _logger.LogInformation($"IP address is not a valid IP address: {clientIp}.");
                return new BadRequestObjectResult($"IP address is not a valid IP address: {clientIp}");
            }
        }

        private class IpApiResult
        {
            public string? Status { get; set; }
            public string? Message { get; set; }
            public string? Country { get; set; }
            public string? RegionName { get; set; }
            public string? City { get; set; }
            public string? Zip { get; set; }
            public double? Lat { get; set; }
            public double? Lon { get; set; }
            public string? Timezone { get; set; }
            public int? Offset { get; set; }
            public string? Isp { get; set; }
            public string? Org { get; set; }
            public string? As { get; set; }
            public string? Query { get; set; }
        }
    }

}
