using System.Threading.Tasks;
using XBridge.Host.XBridge.Core;
using XBridge.Host.XBridge.Core.Models;
using Xunit;


namespace XBridge.Host.XBridge.Tests
{
public class OptimizeManagerTests
{
[Fact]
public async Task OptimizeDevice_SetsVersionAndStatus()
{
var manager = new OptimizeManager();
var device = new DeviceSession { DeviceSerial = "123", DeviceName = "Device1" };


var dto = await manager.OptimizeDeviceAsync(device);


Assert.NotNull(dto);
Assert.Equal("123", dto.DeviceSerial);
Assert.Equal("optimized", dto.Status);
Assert.StartsWith("opt-", dto.Version);
}
}
}