using System;


namespace XBridge.Host.XBridge.Core.Models
{
public class DeviceSession
{
public string DeviceSerial { get; set; }
public string DeviceName { get; set; }
public double AssignedSharePercent { get; set; }
public string OptimizeVersion { get; set; }
public DateTime LastSeen { get; set; }
public object MicrobenchmarkResults { get; set; }
}
}