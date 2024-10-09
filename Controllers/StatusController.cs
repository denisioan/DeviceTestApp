using DeviceTestApp.Services;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DeviceTestApp.Controllers;

public class StatusController
{
    private readonly DeviceService _deviceService;

    public StatusController(DeviceService deviceService)
    {
        _deviceService = deviceService;
    }

    // Handle the /status request to get the device status
    public async Task HandleStatusRequest(HttpListenerRequest request, HttpListenerResponse response)
    {
        string authHeader = request.Headers["Authorization"];
        if (!_deviceService.ValidateToken(authHeader))
        {
            response.StatusCode = (int)HttpStatusCode.Unauthorized;
            return;
        }

        // Get the current status from the DeviceService
        string status = _deviceService.GetDeviceStatus();

        // Create the response object
        var statusResponse = new
        {
            status = status
        };

        // Send the response as JSON
        Console.WriteLine("Handle StatusRequest");
        byte[] responseBuffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(statusResponse));
        response.ContentType = "application/json";
        response.ContentLength64 = responseBuffer.Length;
        await response.OutputStream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
    }
}

