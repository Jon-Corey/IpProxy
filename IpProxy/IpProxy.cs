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
                return new OkObjectResult(new ResponseModel("success"));
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

                        var result = await response.Content.ReadFromJsonAsync<ResponseModel>();

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
                        var result = new ObjectResult(new ResponseModel("fail", body));

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
                    var result = new ObjectResult(new ResponseModel("fail", "Request to IP-API.com timed out."));
                    result.StatusCode = StatusCodes.Status504GatewayTimeout;
                    return result;
                }
            }
            else
            {
                _logger.LogInformation($"IP address is not a valid IP address: {clientIp}.");
                return new BadRequestObjectResult(new ResponseModel("fail", $"IP address is not a valid IP address: {clientIp}"));
            }
        }

        private class ResponseModel
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

            public ResponseModel(
                string? status = null,
                string? message = null,
                string? country = null,
                string? regionName = null,
                string? city = null,
                string? zip = null,
                double? lat = null,
                double? lon = null,
                string? timezone = null,
                int? offset = null,
                string? isp = null,
                string? org = null,
                string? @as = null,
                string? query = null)
            {
                Status = status;
                Message = message;
                Country = country;
                RegionName = regionName;
                City = city;
                Zip = zip;
                Lat = lat;
                Lon = lon;
                Timezone = timezone;
                Offset = offset;
                Isp = isp;
                Org = org;
                As = @as;
                Query = query;
            }
        }
    }

}
