using DeviceTestApp.Models;
using DeviceTestApp.Services;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DeviceTestApp.Controllers;

public class DepositController
{
    private readonly DeviceService _deviceService;

    public DepositController(DeviceService deviceService)
    {
        _deviceService = deviceService;
    }

    public async Task<bool> HandleDepositRequest(HttpListenerRequest request, HttpListenerResponse response)
    {
        string authHeader = request.Headers["Authorization"];
        if (!_deviceService.ValidateToken(authHeader))
        {
            response.StatusCode = (int)HttpStatusCode.Unauthorized;
            return false;
        }

        Console.WriteLine("Handle DepositRequest");
        using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
        {
            string requestBody = await reader.ReadToEndAsync();
            var depositRequest = JsonConvert.DeserializeObject<DepositRequest>(requestBody);

            // Handle deposit using DeviceService
            var depositResponse = _deviceService.Deposit(depositRequest.Amount);

            byte[] responseBuffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(depositResponse));
            response.ContentType = "application/json";
            response.ContentLength64 = responseBuffer.Length;
            await response.OutputStream.WriteAsync(responseBuffer, 0, responseBuffer.Length);

            return depositResponse.Success;
        }
    }
}
