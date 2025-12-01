; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md


## Release 1.1.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
LAI005  |  Design  |  Error   | "Replace = true" requires a specified service type (ServiceType).
LAI006  |  Design  |  Error   | Multiple `AutoInjectExtensions` declarations found; only one is allowed.
LAI007  |  Design  |  Error   | Generated extension host must be a `static partial` class.
LAI103  |  Design  |  Warning | `Replace` usage is not supported by the referenced DI Abstractions version.
LAI104  |  Design  |  Warning | Assembly-level `AutoInjectConfig` is unnecessary or redundant in this context.


## Release 1.0.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
LAI001  |  Design  |  Error   | Duplicate service attribute.
LAI002  |  Design  |  Error   | Conflicting lifetimes.
LAI003  |  Design  |  Error   | Service type mismatch.
LAI004  |  Design  |  Error   | Invalid naming of methods, classes, or namespaces.
LAI101  |  Design  |  Warning | Keyed service registrations require Microsoft.Extensions.DependencyInjection.Abstractions >= 8.0.0.
LAI102  |  Design  |  Warning | Conflicting extension method name/signature with generated AutoInject extension.
