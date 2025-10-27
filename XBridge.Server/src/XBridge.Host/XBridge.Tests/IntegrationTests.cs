using System.Threading.Tasks;
using XBridge.Host.XBridge.Core;
using XBridge.Host.XBridge.Core.Models;
using Xunit;


namespace XBridge.Host.XBridge.Tests
{
public class IntegrationTests
{
[Fact]
public async Task FullIntegrationTest()
{
var sessionManager = new SessionManager();
var optimizeManager = new OptimizeManager();
sessionManager.Register("123", "Device1");


var device = sessionManager.Get("123");
var dto = await optimizeManager.OptimizeDeviceAsync(device);


Assert.Equal(device.DeviceSerial, dto.DeviceSerial);
Assert.Equal("optimized", dto.Status);
}
}
}