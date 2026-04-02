# 🏴‍☠️ ChaosSeas

Chaos engineering demo for .NET with Polly v8 and .NET Aspire.

## About

Demo application for the [CodeStock 2026](https://codestock.org/) session **"Polly Want a 500? Chaos Engineering for .NET Apps"** by [Victor Frye](https://github.com/VictorFrye).

It demonstrates progressive chaos engineering with Polly v8 chaos strategies, `Microsoft.Extensions.Http.Resilience`, and feature flags via `Microsoft.FeatureManagement` — all orchestrated with .NET Aspire.

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [.NET Aspire workload](https://learn.microsoft.com/dotnet/aspire/fundamentals/setup-tooling)
  ```bash
  dotnet workload install aspire
  ```
- A container runtime ([Docker Desktop](https://www.docker.com/products/docker-desktop/) or [Podman](https://podman.io/))

## Getting Started

```bash
git clone https://github.com/VictorFrye/ChaosSeas.git
cd ChaosSeas
dotnet run --project src/AppHost
```

Open the Aspire dashboard URL shown in the console to observe traces, logs, and metrics.

## Demo Scenarios

### Demo 1: Calm Seas

```
GET /voyage/calm-seas
```

Shows `AddStandardResilienceHandler()` protecting against an unreliable downstream. Watch retries and circuit breaker behavior in the Aspire dashboard.

### Demo 2: Rough Seas

```
GET /voyage/rough-seas/{faults|latency|outcomes}
```

Shows Polly chaos strategies injecting faults (50%), latency (50%), and bad outcomes (50%). All visible in Aspire traces.

### Demo 3: Game Day

```
GET /voyage/game-day
GET /voyage/game-day/status
```

Shows feature-flag-controlled chaos. Toggle flags in `appsettings.json` to arm/disarm chaos during a live game day exercise.

## Feature Flags

Configure chaos injection in `src/HarborApi/appsettings.json`:

```json
{
  "FeatureManagement": {
    "ChaosEnabled": false,
    "ChaosFault": false,
    "ChaosLatency": false,
    "ChaosOutcome": false
  }
}
```

Set `ChaosEnabled` to `true` to arm the system, then toggle individual flags to control which chaos strategies are active. Hot-reload is supported — no restart required.

## Using the Voyager CLI

Run automated demo scenarios with the Voyager CLI while the AppHost is running:

```bash
dotnet run --project src/Voyager -- calm-seas 20
dotnet run --project src/Voyager -- rough-seas-faults 10
dotnet run --project src/Voyager -- game-day 15
```

## Running Tests

```bash
dotnet test
```

## Built With

- [.NET 10](https://dotnet.microsoft.com/)
- [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/)
- [Polly v8](https://www.thepollyproject.org/) via `Microsoft.Extensions.Http.Resilience`
- [Microsoft.FeatureManagement](https://learn.microsoft.com/azure/azure-app-configuration/feature-management-dotnet-reference)
- [xUnit v3](https://xunit.net/)

## License

This project is licensed under the [MIT License](LICENSE).
