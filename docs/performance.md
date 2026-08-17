# Performance profile

The v5 layout engine replaces per-week, per-item LINQ overlap scans with one range sort, week partitioning, and compact seven-day occupancy masks. Pointer handling also moved from a global handler that measured and sorted every day element on each throttled move to a per-instance module that calculates the grid cell and invokes .NET only when the day changes.

## Reproduce

Run the release profiler on the same machine before comparing results:

```powershell
dotnet run --project BlazorScheduler.Profiling/BlazorScheduler.Profiling.csproj -c Release
```

The fixture uses a deterministic mix of timed and multi-day appointments across a 42-day range. Each value is the median of five runs after warm-up. The legacy comparison mirrors the v4 ordering and render-time overlap scans; it isn't included in production code.

## Results

Results are captured during the v5 implementation and are informational, not a CI gate.

Measured on Windows x64 with .NET SDK 10.0.302:

| Items | Legacy median | v5 median | Speedup | v5 allocation |
| ---: | ---: | ---: | ---: | ---: |
| 50 | 0.23 ms | 0.04 ms | 6.2× | 28.8 KiB |
| 500 | 14.51 ms | 0.26 ms | 54.8× | 183.1 KiB |
| 5,000 | 283.97 ms | 2.09 ms | 136.1× | 1,776.0 KiB |

Browser profiling should additionally verify that drag handling emits at most one interop call per newly entered day and that multiple scheduler instances maintain separate pointer handlers.
