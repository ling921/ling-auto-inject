; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md


## Release 1.1.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
LAI005  |  Design  |  Error   | Required service type for Replace registration.
LAI103  |  Design  |  Warning | Not supported replace service registration.


## Release 1.0.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
LAI001  |  Design  |  Error   | Duplicate service attribute.
LAI002  |  Design  |  Error   | Conflicting lifetimes.
LAI003  |  Design  |  Error   | Service type mismatch.
LAI004  |  Design  |  Error   | Invalid AutoInject configuration.
LAI101  |  Design  |  Warning | Not supported keyed service registration.
LAI102  |  Design  |  Warning | Conflicting extension method.
