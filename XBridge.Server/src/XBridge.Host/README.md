# XBridge.Host


XBridge.Host is the Xbox server application component for the XBridge project. It manages device connections, distributes optimization tasks, and coordinates results from multiple devices in a local network environment.


## Features
- Connect multiple devices over TCP JSON or gRPC (optional)
- Track device sessions and optimize performance tasks
- Force global optimization or per-device optimization
- Monitor last seen and microbenchmark results
- Configurable via `xbridge_config.json`


## Setup
1. Install .NET 7.0 SDK
2. Build the solution using `build.ps1`
3. Package for deployment using `package_msix.ps1`
4. Configure `xbridge_config.json` for ports and store paths


## Usage
- Start the application
- Connect devices via their native integration
- Monitor and manage optimizations through GUI


## Testing
- Unit tests located in `XBridge.Tests`
- Integration tests ensure multiple device handling


## Documentation
- See `docs/deployment.md` for deployment steps
- See `docs/api_spec.md` for API message definitions
- See `docs/acceptance_tests.md` for testing criteria