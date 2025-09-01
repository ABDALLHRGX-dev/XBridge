# XBridge.Host — Combined & Enhanced Documentation

> Clean, navigable single-file reference for **XBridge.Host** — server side component of the XBridge project.

<!-- Table of Contents -->

## Table of Contents

1. [Overview](#overview)
2. [Features](#features)
3. [Quickstart & Setup](#quickstart--setup)
4. [Configuration](#configuration)
5. [API — TCP JSON (examples)](#api--tcp-json-examples)
6. [gRPC / Protobuf](#grpc--protobuf)
7. [Acceptance Tests](#acceptance-tests)
8. [Security & Best Practices](#security--best-practices)
9. [Utilities & Useful Commands](#utilities--useful-commands)
10. [Proto Definitions (proto/xbridge.proto)](#proto-definitions-protoxbridgeproto)
11. [Changelog & Final Notes](#changelog--final-notes)

---

## Overview

**XBridge.Host** is the server component intended to run on Windows/Xbox (Dev Mode). It accepts connections from multiple client devices, assigns/coordinates optimization work, collects microbenchmark results, and stores optimization artifacts for distribution.

This single Markdown file is the canonical project reference for README, deployment steps, API examples (TCP JSON), acceptance tests, and protobuf definitions.

---

## Features

* Lightweight TCP JSON transport; optional gRPC/Protobuf for production.
* Device session tracking and last-seen / health monitoring.
* Per-device or global optimization flows with persisted optimize files.
* Microbenchmark / measurement collection and result aggregation.
* Configurable store paths and ports via `xbridge_config.json`.
* Simple GUI for device listing and manual actions (Force Optimize, etc.).

---

## Quickstart & Setup

### Prerequisites

* Windows 10/11 (or Xbox in Dev Mode).
* .NET 7.0 SDK (runtime + SDK for building).
* Visual Studio 2022 (UWP/WinUI workload) **if** packaging UI as MSIX/AppX.

### Build & Package

1. Clone the repo: `git clone <repo-url>`
2. Build (PowerShell):

```powershell
# الكود الأول — build
./build/build.ps1
```

3. Package (if producing MSIX/AppX):

```powershell
# الكود الثاني — package
./build/package_msix.ps1
```

4. Place `xbridge_config.json` beside the running binary (or configure a known path) and adjust ports/store paths.
5. Deploy the produced MSIX/AppX to target device (Dev Mode required on Xbox).

---

## Configuration

A minimal `xbridge_config.json` example (adjust ports and paths):

```json
{
  "listen_port": 50051,
  "optimize_store_path": "C:/XBridge/optimize_store",
  "auth_tokens": ["shared-secret-token-1"],
  "transport": "tcp_json",
  "max_concurrent_tasks": 8
}
```

Make sure `optimize_store_path` exists and is writable by the process account.

---

## API — TCP JSON (examples)

**Notes**: Messages are UTF-8 encoded JSON when using TCP JSON transport. For production, consider gRPC for schema guarantees and binary efficiency.

### Hello (device -> server)

```json
// الكود الأول — Hello
{
  "type": "hello",
  "device_name": "TestPhone_A",
  "device_serial": "SN-TEST-A001",
  "game_full_version": "FortniteClient_38.20.1234_build_98765",
  "capabilities": { "cpu_cores": 4, "ram_mb": 3000, "neon": true },
  "auth_token": "optional-token"
}
```

### Heartbeat (device -> server)

```json
// الكود الثاني — Heartbeat
{
  "type": "heartbeat",
  "device_serial": "SN-TEST-A001",
  "timestamp_ms": 1696070400000
}
```

### Work Request (device -> server)

```json
// الكود الثالث — Work Request
{
  "type": "work_request",
  "device_serial": "SN-TEST-A001",
  "task_id": "task-0001",
  "task_type": "physics_sim",
  "payload": "base64-or-json-payload",
  "timeout_ms": 1200
}
```

### Work Response (server -> device)

```json
// الكود الرابع — Work Response
{
  "type": "work_response",
  "task_id": "task-0001",
  "success": true,
  "result": "base64-or-json-result",
  "error": null
}
```

---

## gRPC / Protobuf (recommended for production)

If you enable gRPC, keep `proto/xbridge.proto` as the canonical schema and regenerate stubs per language. Advantages: strict typing, smaller messages, built-in deadlines and retries.

---

## Acceptance Tests — XBridge.Host

This section contains structured acceptance tests used to validate functional and non-functional requirements before promoting a build to test/QA.

### Preconditions

* Target device: Windows 10/11 or Xbox (Dev Mode).
* `dotnet` in PATH and .NET 7 SDK installed.
* `xbridge_config.json` present and valid.
* `optimize_store_path` exists and writable.

### Test Message Samples (use with a simple TCP JSON client or `nc`)

(See the `Hello`, `Heartbeat`, `Work Request`, `Work Response` samples above.)

### Test Cases (TC)

| ID    | Title                             | Goal                                            | Pass Criteria                                                            |
| ----- | --------------------------------- | ----------------------------------------------- | ------------------------------------------------------------------------ |
| TC-01 | Basic Connection (Hello)          | Server accepts new device and shows it in GUI   | Device appears within 5s with DeviceName & DeviceSerial                  |
| TC-02 | Heartbeat Updates LastSeen        | Verify `LastSeen` updates on heartbeat          | `LastSeen` difference < 10s during periodic heartbeats                   |
| TC-03 | Auto Optimize on Version Mismatch | Server triggers optimize when versions mismatch | `optimize_<serial>.json` created within 60s and contains `device_serial` |
| TC-04 | Resource Sharing Two Devices      | Equal split when two devices connected          | AssignedSharePercent \~50% (49–51%)                                      |
| TC-05 | Force Optimize per Device         | GUI Force Optimize creates new optimize version | `OptimizeVersion` updated within 30s                                     |
| TC-06 | Work Request/Response Roundtrip   | Work request yields valid response              | `work_response.success = true` with matching `task_id`                   |
| TC-07 | Disconnect & Reconnect            | Client handles server downtime and reconnects   | Client reappears in GUI within 120s after restart                        |
| TC-08 | Replace Old Optimize              | New optimize replaces previous                  | Only current usable optimize exists (unless retention configured)        |
| TC-09 | Basic Load                        | Server handles N simulated devices              | Average request latency < 200ms under light LAN load (env dependent)     |

**Pass to Release**: All TC-01..TC-09 must be `Pass` or have recorded failures & remediation plan.

---

## Security & Best Practices

* Run inside a closed LAN for testing.
* Require `auth_token` on client messages; validate server-side.
* Sign or checksum optimize artifacts (HMAC/SHA256) prior to distribution.
* Use TLS or mTLS if bridging across untrusted networks.
* Limit concurrently running tasks (`max_concurrent_tasks`) to avoid CPU thrash.

---

## Utilities & Useful Commands

**PowerShell SHA256**

```powershell
Get-FileHash .\optimize_SN-TEST-A001.json -Algorithm SHA256
```

**Linux / macOS**

```bash
sha256sum optimize_SN-TEST-A001.json
```

**Quick netcat send (manual test)**

```bash
# send a Hello JSON to localhost:50051
cat hello.json | nc 127.0.0.1 50051
```

**Python TCP JSON client (skeleton)**

```python
# الكود الخامس — simple client
import socket, json
s = socket.create_connection(("127.0.0.1", 50051))
msg = json.dumps({"type":"hello","device_name":"cli","device_serial":"SN-1"}) + "\n"
s.send(msg.encode())
print(s.recv(4096).decode())
s.close()
```

---

## Proto Definitions (proto/xbridge.proto)

```protobuf
syntax = "proto3";
package xbridge;

message HelloRequest {
  string device_serial = 1;
  string device_name = 2;
  string game_full_version = 3;
  string auth_token = 4;
}

message HelloResponse {
  string status = 1;
  string optimize_version = 2;
}

message WorkRequest {
  string device_serial = 1;
  string task_id = 2;
  string task_type = 3;
  bytes payload = 4;
  int32 timeout_ms = 5;
}

message WorkResponse {
  string task_id = 1;
  bool success = 2;
  bytes result = 3;
  string error = 4;
}

service XBridgeService {
  rpc SendHello(HelloRequest) returns (HelloResponse);
  rpc SendWork(WorkRequest) returns (WorkResponse);
  rpc Heartbeat(HelloRequest) returns (HelloResponse);
}
```

---

## Changelog & Final Notes

* Keep this file as the canonical documentation for the Host component.
* For production, prefer gRPC for tight schema control and better performance.
* Add CI steps to run test simulations (TC-09) and artifact checksum verification.

---

*If you want this file exported as `README.md` or split into `README.md`, `API.md`, and `ACCEPTANCE_TESTS.md`, tell me which split you prefer and I will create them.*
  "game_full_version": "FortniteClient_38.20.1234_build_98765",
  "capabilities": { "cpu_cores": "4", "ram_mb": "3000", "neon": "true" },
  "auth_token": "optional-token"
}
````

#### Heartbeat (device -> server)

```json
{
  "type": "heartbeat",
  "device_serial": "SN-TEST-A001"
}
```

#### Work Request (device -> server)

```json
{
  "type": "work_request",
  "device_serial": "SN-TEST-A001",
  "task_id": "task-0001",
  "task_type": "physics_sim",
  "payload": "base64-or-json-payload",
  "timeout_ms": 1200
}
```

#### Work Response (server -> device)

```json
{
  "type": "work_response",
  "task_id": "task-0001",
  "success": true,
  "result": "base64-or-json-result",
  "error": null
}
```

### gRPC Alternative

If gRPC/protobuf is enabled, use `proto/xbridge.proto` definitions and generate language-specific stubs. gRPC is recommended for performance and strict schema enforcement in production scenarios.

---

## Acceptance Tests — XBridge Host

This document defines the detailed Acceptance Tests for the XBridge.Host server component. The tests validate that the system meets functional and non-functional requirements before promotion to a test environment or Dev Mode deployment.

### 1. Pre-test Requirements

* Target device: Windows 10/11 or Xbox in Dev Mode.
* .NET 7 SDK installed and `dotnet` available in PATH.
* Project built (Release) using `build/build.ps1` or `dotnet build`.
* `xbridge_config.json` is present at `Bridge/XBridge.Server/src/XBridge.Host/xbridge_config.json`.
* `optimize_store_path` exists and is writable.
* Test tools: PowerShell, `nc`/`netcat`, or a simple TCP JSON client (Python/C#) to simulate devices.

### 2. Test Message Samples (JSON)

These messages are sent over TCP (UTF-8) to the listening port configured in `xbridge_config.json`. If gRPC is enabled, use the protobuf equivalents.

**Hello**

```json
{
  "type": "hello",
  "device_name": "TestPhone_A",
  "device_serial": "SN-TEST-A001",
  "game_full_version": "FortniteClient_38.20.1234_build_98765",
  "capabilities": { "cpu_cores": "4", "ram_mb": "3000", "neon": "true" }
}
```

**Heartbeat**

```json
{
  "type": "heartbeat",
  "device_serial": "SN-TEST-A001"
}
```

**Work Request**

```json
{
  "type": "work_request",
  "device_serial": "SN-TEST-A001",
  "task_id": "task-0001",
  "task_type": "physics_sim",
  "payload": "base64-or-json-payload",
  "timeout_ms": 1200
}
```

**Work Response**

```json
{
  "type": "work_response",
  "task_id": "task-0001",
  "success": true,
  "result": "base64-or-json-result"
}
```

### 3. Test Cases

#### TC-01 — Basic Connection (Hello)

* **Goal:** Server accepts new device and displays it in GUI.
* **Preconditions:** Server running, listen port open (e.g., 50051).
* **Steps:** Send `hello` to server\:PORT; observe GUI.
* **Expected:** New entry shows `DeviceName`, `DeviceSerial`, `AssignedSharePercent = 100%`.
* **Pass Criteria:** Device appears within 5 seconds.

#### TC-02 — Heartbeat Updates LastSeen

* **Goal:** Verify `LastSeen` updates on heartbeat.
* **Steps:** Send `hello`, then send `heartbeat` every 5s for 20s.
* **Expected:** `LastSeen` updates accordingly.
* **Pass Criteria:** Difference between current time and `LastSeen` < 10s during heartbeat transmissions.

#### TC-03 — Auto Optimize on Version Mismatch

* **Goal:** If `game_full_version` differs between device and server optimize DB, server triggers or requests optimize.
* **Steps:** Send `hello` with older `game_full_version`.
* **Expected:** Server initiates optimize flow and persists `optimize_<serial>.json`.
* **Pass Criteria:** `optimize_<serial>.json` exists within 60s and contains matching `device_serial`.

#### TC-04 — Resource Sharing Between Two Devices (Fixed Split)

* **Goal:** Equal share assignment when two devices connected.
* **Steps:** Connect device A and B.
* **Expected:** Each device shows `AssignedSharePercent ≈ 50%`.
* **Pass Criteria:** Values within 49–51%.

#### TC-05 — Force Optimize per Device

* **Goal:** GUI's `Force Optimize` updates optimize version and store.
* **Steps:** Select device in GUI and click `Force Optimize`.
* **Expected:** `OptimizeVersion` updated (prefix `opt-`), new optimize file created/updated.
* **Pass Criteria:** `OptimizeVersion` reflects change within 30s.

#### TC-06 — Work Request/Response Roundtrip

* **Goal:** Device sends work\_request and receives valid work\_response.
* **Steps:** Send valid `work_request`.
* **Expected:** `work_response.success = true` and matching `task_id`.
* **Pass Criteria:** Response arrives within `timeout_ms` or failure reported clearly.

#### TC-07 — Server Disconnect and Reconnect Behavior

* **Goal:** Client (libnative) handles server downtime and retries.
* **Steps:** Connect device, then stop server; observe client; restart server.
* **Expected:** Client stops sending requests and probes every 5s; reconnects when server is back.
* **Pass Criteria:** Client reappears in GUI within 120s after server restart.

#### TC-08 — Replace Old Optimize with New

* **Goal:** Old optimize file is replaced when a new optimize is generated.
* **Steps:** Generate `optimize_OLD` then force new optimize.
* **Expected:** New optimize replaces previous file or is updated.
* **Pass Criteria:** Only current usable optimize exists for device unless configured otherwise.

#### TC-09 — Basic Load/Performance Test

* **Goal:** Verify server handles N simulated devices.
* **Steps:** Simulate 10 (or target N) devices connecting and sending work\_requests.
* **Expected:** Average response time for `work_request` within acceptable LAN limits.
* **Pass Criteria:** Average < 200ms under light LAN load (reference value; adjust for environment).

### 4. Security and Safety Requirements for Testing

* Perform tests inside a closed local network.
* Each device should provide `auth_token` defined in `xbridge_config.json`.
* Verify HMAC/SHA256 checksum on optimize files when transferred.

**Useful commands**

* PowerShell SHA256:

```powershell
Get-FileHash .\optimize_SN-TEST-A001.json -Algorithm SHA256
```

* Linux/macOS SHA256:

```bash
sha256sum optimize_SN-TEST-A001.json
```

### 5. Suggested Simulation Tools

* Python small TCP JSON client script to simulate devices.
* `netcat` (nc) for quick manual message sends.
* PowerShell with `System.Net.Sockets.TcpClient` for Windows-based scripts.

### 6. Exit Criteria / Acceptance

Before marking the release as acceptable:

1. All test cases (TC-01..TC-09) must be **Pass** or have documented failures and a remediation plan.
2. Optimize files are produced and stored in `optimize_store_path` with correct SHA256 checksums.
3. Server disconnect/reconnect behavior works as specified (TC-07).
4. Resource sharing follows fixed-split policy (TC-04).
5. No catastrophic server errors during basic load tests.

### 7. Logging and Reporting

* Record each test result in CSV/JSON: `test_id, status, timestamp, notes, logs_path`.
* Keep server logs (audit) per session for debugging on failures.

### 8. Notes and Extensions

* Tests can be extended to include deeper security audits, resource usage checks (CPU/Memory), packet loss and latency simulations.
* Any change in the transport (JSON ↔ Protobuf) requires updating test messages accordingly.

---

## Proto Definitions (proto/xbridge.proto)

```protobuf
syntax = "proto3";
package xbridge;

message HelloRequest {
  string device_serial = 1;
  string device_name = 2;
  string game_full_version = 3;
  string auth_token = 4;
}

message HelloResponse {
  string status = 1;
  string optimize_version = 2;
}

message WorkRequest {
  string device_serial = 1;
  string task_id = 2;
  string task_type = 3;
  bytes payload = 4;
  int32 timeout_ms = 5;
}

message WorkResponse {
  string task_id = 1;
  bool success = 2;
  bytes result = 3;
  string error = 4;
}

service XBridgeService {
  rpc SendHello(HelloRequest) returns (HelloResponse);
  rpc SendWork(WorkRequest) returns (WorkResponse);
  rpc Heartbeat(HelloRequest) returns (HelloResponse);
}
```

---

## Final Notes

* Keep this single Markdown file with the project for easy reference.
* Use this document as the canonical reference for README, Deployment, API specification, and Acceptance Tests.

```
::contentReference[oaicite:0]{index=0}
```
