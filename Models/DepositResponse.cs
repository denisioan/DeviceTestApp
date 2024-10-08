using System;

namespace DeviceTestApp.Models;

// Response model for deposit operations
public class DepositResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
}
