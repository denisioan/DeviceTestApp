using DeviceTestApp.Models;
using DeviceTestApp.OAuth;
using System;

namespace DeviceTestApp.Services;

public class DeviceService
{
    private readonly JwtTokenService _tokenService;
    private bool _isDeviceOperational;

    public DeviceService(JwtTokenService tokenService)
    {
        _tokenService = tokenService;
        _isDeviceOperational = true;
    }

    // Validate the provided JWT token (from the Authorization header)
    public bool ValidateToken(string authHeader)
    {
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith(OAuthServer.TokenType + " "))
        {
            return false;
        }
        string token = authHeader.Substring(OAuthServer.TokenType.Length+1).Trim();
        return _tokenService.ValidateToken(token);
    }

    // Handle the deposit operation
    public DepositResponse Deposit(int amount)
    {
        if (!_isDeviceOperational)
        {
            return new DepositResponse
            {
                Success = false,
                Message = "Device is not operational."
            };
        }

        if (amount <= 0)
        {
            return new DepositResponse
            {
                Success = false,
                Message = "Deposit amount must be greater than zero."
            };
        }

        // Simulate deposit operation
        Console.WriteLine($"Deposited {amount} units successfully.");

        return new DepositResponse
        {
            Success = true,
            Message = $"Deposited {amount} units successfully."
        };
    }

    // Get the current status of the device
    public string GetDeviceStatus()
    {
        return _isDeviceOperational ? "operational" : "out of service";
    }

    // Optionally, provide a method to simulate device failure or maintenance
    public void SetDeviceOperationalStatus(bool status)
    {
        _isDeviceOperational = status;
    }
}
