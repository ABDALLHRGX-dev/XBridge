using System;
using System.Collections.Generic;
using XBridge.Host.XBridge.Core.Models;


namespace XBridge.Host.XBridge.Core
{
public class SessionManager
{
readonly Dictionary<string, DeviceSession> _sessions = new Dictionary<string, DeviceSession>();


public void Register(string serial, string name)
{
_sessions[serial] = new DeviceSession { DeviceSerial = serial, DeviceName = name, LastSeen = DateTime.UtcNow, AssignedSharePercent = 100 };
}


public bool Exists(string serial) => _sessions.ContainsKey(serial);


public DeviceSession Get(string serial) => _sessions.TryGetValue(serial, out var s) ? s : null;


public IEnumerable<DeviceSession> GetAll() => _sessions.Values;
}
}