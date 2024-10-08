using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DeviceTestApp.OAuth;

public class OAuthServer
{
    private readonly JwtTokenService _tokenService;

    private readonly string ClientId = "armorsafe_client_id";      // Replace with actual client ID
    private readonly string ClientSecret = "armorsafe_client_secret"; // Replace with actual client secret
    private static readonly int TokenExpiration = 3600;
    public static readonly string TokenType = "Bearer";

    public OAuthServer(JwtTokenService tokenService)
    {
        _tokenService = tokenService;
    }

    // Handle /token endpoint for client credentials grant
    public async Task HandleTokenRequest(HttpListenerRequest request, HttpListenerResponse response)
    {
        using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
        {
            // Read the form data
            string requestBody = await reader.ReadToEndAsync();
            var form = System.Web.HttpUtility.ParseQueryString(requestBody);
            string clientId = form["client_id"];
            string clientSecret = form["client_secret"];

            // Validate client credentials
            if (clientId == ClientId && clientSecret == ClientSecret)
            {
                // Generate the JWT token
                string token = _tokenService.GenerateToken(clientId, DateTime.Now.AddSeconds(TokenExpiration));
                var tokenResponse = new
                {
                    access_token = token,
                    token_type = TokenType,
                    expires_in = TokenExpiration
                };

                // Send token response
                response.StatusCode = (int)HttpStatusCode.OK;
                response.ContentType = "application/json";
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(tokenResponse));
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
            else
            {
                // Invalid client credentials
                Console.WriteLine($"HandleTokenRequest: Invalid credentials");
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
            }
        }
    }
}
