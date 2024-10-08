using DeviceTestApp.Services;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DeviceTestApp.Controllers;

public class ThingDescriptionController
{
    private readonly DeviceService _deviceService;

    public ThingDescriptionController(DeviceService deviceService)
    {
        _deviceService = deviceService;
    }

    // Handle the /thing-description request to provide the Thing Description
    public async Task HandleThingDescriptionRequest(HttpListenerResponse response)
    {
        // Create a Thing Description object in JSON-LD format
        var thingDescription = new
        {
            @context = "https://www.w3.org/2019/wot/td/v1",
            id = "urn:dev:wot:cashdepositdevice-1234",
            title = "Cash Deposit Device",
            description = "A Web of Things test application for a cash deposit device.",
            securityDefinitions = new
            {
                oauth2_sc = new
                {
                    scheme = "oauth2",
                    flow = "client_credentials",
                    token = "http://localhost:8080/token",
                    scopes = new[] { "deposit", "status" }
                }
            },
            security = new[] { "oauth2_sc" },
            properties = new
            {
                status = new
                {
                    type = "string",
                    description = "The current status of the device",
                    forms = new[]
                    {
                        new
                        {
                            href = "http://localhost:8080/status",
                            contentType = "application/json",
                            op = new[] { "readproperty" }
                        }
                    }
                }
            },
            actions = new
            {
                deposit = new
                {
                    description = "Deposit an amount of money into the cash deposit device.",
                    input = new
                    {
                        type = "object",
                        properties = new
                        {
                            amount = new
                            {
                                type = "integer",
                                minimum = 1
                            }
                        }
                    },
                    forms = new[]
                    {
                        new
                        {
                            href = "http://localhost:8080/deposit",
                            contentType = "application/json",
                            op = new[] { "invokeaction" }
                        }
                    }
                }
            }
        };

        // Send the Thing Description as JSON-LD
        byte[] responseBuffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(thingDescription, Formatting.Indented));
        response.ContentType = "application/json";
        response.ContentLength64 = responseBuffer.Length;
        await response.OutputStream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
    }
}

