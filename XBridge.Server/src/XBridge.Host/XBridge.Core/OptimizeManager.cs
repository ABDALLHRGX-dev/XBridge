using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using XBridge.Host.XBridge.Core.Models;


namespace XBridge.Host.XBridge.Core
{
public class OptimizeManager
{
readonly Dictionary<string, OptimizeDto> _store = new Dictionary<string, OptimizeDto>();


public OptimizeDto GetOptimization(string deviceSerial)
{
if (_store.TryGetValue(deviceSerial, out var dto)) return dto;
return null;
}


public void SaveOptimization(string deviceSerial, OptimizeDto dto)
{
_store[deviceSerial] = dto;
}


public Task<OptimizeDto> OptimizeDeviceAsync(DeviceSession device)
{
var dto = new OptimizeDto
{
DeviceSerial = device.DeviceSerial,
Version = "opt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
Status = "optimized"
};
SaveOptimization(device.DeviceSerial, dto);
return Task.FromResult(dto);
}
}
}