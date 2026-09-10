using Ling.AutoInject.SourceGenerators.Extensions;
using Ling.AutoInject.SourceGenerators.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Text;

namespace Ling.AutoInject.SourceGenerators;

/// <summary>
/// Source generator to generate IServiceCollection extension methods for AutoInject attributes.
/// </summary>
[Generator(LanguageNames.CSharp)]
internal sealed class AutoInjectGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Generate attribute definitions
        context.RegisterPostInitializationOutput(GenerateAttributes);

        var classDeclarations = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (node, _) => node is TypeDeclarationSyntax tds && tds.AttributeLists.Count > 0,
            transform: static (ctx, _) =>
            {
                var typeDecl = (TypeDeclarationSyntax)ctx.Node;
                var model = ctx.SemanticModel;
                if (model.GetDeclaredSymbol(typeDecl) is INamedTypeSymbol namedTypeSymbol)
                {
                    var symbols = new AutoInjectSymbols(model.Compilation);
                    var attrs = namedTypeSymbol.GetAttributes()
                        .Where(ad => symbols.IsAutoInjectAttribute(ad.AttributeClass))
                        .ToImmutableArray();
                    var options = namedTypeSymbol.GetAttributes()
                        .Where(ad => symbols.IsAutoOptionsAttribute(ad.AttributeClass))
                        .ToImmutableArray();
                    if (attrs.Length > 0 || options.Length > 0)
                    {
                        return new ClassWithAttributes(namedTypeSymbol, attrs, options);
                    }
                }

                return null;
            })
            .Where(static x => x != null)
            .Collect();

        var compilationAndClasses = context.CompilationProvider.Combine(context.AnalyzerConfigOptionsProvider.Combine(classDeclarations));

        context.RegisterSourceOutput(compilationAndClasses, (spc, source) => Execute(spc, source.Left, source.Right.Right!, source.Right.Left));
    }

    private static void GenerateAttributes(IncrementalGeneratorPostInitializationContext context)
    {
        context.AddSourceWithCrlf("AutoInjectConfigAttribute.g.cs", SourceCodes.AutoInjectConfigAttribute);
        context.AddSourceWithCrlf("AutoInjectExtensionsAttribute.g.cs", SourceCodes.AutoInjectExtensionsAttribute);
        context.AddSourceWithCrlf("AutoInjectModuleAttribute.g.cs", SourceCodes.AutoInjectModuleAttribute);
        context.AddSourceWithCrlf("AutoInjectAttribute.g.cs", SourceCodes.AutoInjectAttribute);
        context.AddSourceWithCrlf("SingletonServiceAttribute.g.cs", SourceCodes.SingletonServiceAttribute);
        context.AddSourceWithCrlf("ScopedServiceAttribute.g.cs", SourceCodes.ScopedServiceAttribute);
        context.AddSourceWithCrlf("TransientServiceAttribute.g.cs", SourceCodes.TransientServiceAttribute);
    }

    private void Execute(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<ClassWithAttributes> classes,
        AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider)
    {
        var registrations = new List<RegistrationInfo>();
        var options = new List<OptionsInfo>();
        var symbols = new AutoInjectSymbols(compilation);

        var targetVerison = compilation.FindReferenceAssemblyVersionByTypeMetadataName(Constants.ServiceCollectionServiceExtensionsFullName);
        var supportKeyedService = targetVerison >= Constants.SupportKeyedServiceVersion;
        if (supportKeyedService)
        {
            context.AddSourceWithCrlf("AutoInjectKeyedRegistration.g.cs", SourceCodes.KeyedRegistration);
        }

        var visited = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        // Partial declarations must only contribute their attributes once.
        foreach (var cwa in classes)
        {
            var classSymbol = cwa.ClassSymbol;
            if (!visited.Add(classSymbol))
            {
                continue;
            }

            if (IsSupportedRegistrationType(classSymbol))
            {
            var attrData = cwa.Attributes;

            var regList = new List<RegistrationInfo>();
            var serviceRegistrationSet = new HashSet<(INamedTypeSymbol? ServiceType, string? ServiceKey)>();

            foreach (var ad in attrData)
            {
                var isUnifiedAttribute = SymbolEqualityComparer.Default.Equals(ad.AttributeClass, symbols.AutoInjectAttributeSymbol);
                var lifetime = isUnifiedAttribute
                    ? GetLifetime(ad.GetConstructorArgument(0))
                    : symbols.GetLifetime(ad.AttributeClass);
                if (lifetime is null)
                {
                    continue;
                }

                var serviceTypedConstant = ad.GetConstructorArgument(isUnifiedAttribute ? 1 : 0);
                if (ad.NamedArguments.Any(a => a.Key == "ServiceType"))
                {
                    serviceTypedConstant = ad.GetNamedArgument("ServiceType");
                }
                var serviceKeyTypedConstant = ad.GetNamedArgument("ServiceKey");
                var replaceTypedConstant = ad.GetNamedArgument("Replace");
                var strategyTypedConstant = ad.GetNamedArgument("Strategy");
                var registerImplementedInterfacesTypedConstant = ad.GetNamedArgument("RegisterImplementedInterfaces");
                var moduleTypedConstant = ad.GetNamedArgument("Module");

                var serviceType = serviceTypedConstant.Value as INamedTypeSymbol;
                var serviceKey = serviceKeyTypedConstant.IsNull ? null : serviceKeyTypedConstant.ToCSharpString();
                var replace = !replaceTypedConstant.IsNull && replaceTypedConstant.Value is bool b && b;
                var strategy = replace ? "Replace" : GetStrategy(strategyTypedConstant);
                var registerImplementedInterfaces = !registerImplementedInterfacesTypedConstant.IsNull
                    && registerImplementedInterfacesTypedConstant.Value is bool rii
                    && rii;
                var module = moduleTypedConstant.Value as INamedTypeSymbol;

                // Retain the legacy Replace property's pre-8 fallback. The new Strategy
                // uses unkeyed Replace on every supported DI version.
                if (!supportKeyedService)
                {
                    serviceKey = null;
                    if (replace)
                    {
                        strategy = "TryAdd";
                    }
                }

                var serviceTypes = new HashSet<INamedTypeSymbol?>(SymbolEqualityComparer.Default);
                if (registerImplementedInterfaces)
                {
                    serviceTypes.UnionWith(classSymbol.AllInterfaces);
                }

                // A specified service type is additive when interfaces are registered. Without
                // the option it retains the existing self-registration behaviour (null value).
                if (!registerImplementedInterfaces || serviceType is not null)
                {
                    serviceTypes.Add(serviceType);
                }

                foreach (var registrationServiceType in serviceTypes)
                {
                    // Avoid duplicate registrations
                    if (!serviceRegistrationSet.Contains((registrationServiceType, serviceKey)))
                    {
                        regList.Add(new RegistrationInfo(classSymbol, lifetime, registrationServiceType, serviceKey, strategy, module));
                        serviceRegistrationSet.Add((registrationServiceType, serviceKey));
                    }
                }
            }

            registrations.AddRange(regList
                .OrderBy(r => r.ServiceType is null ? 0 : 1)
                .ThenBy(r => r.Strategy == "Replace" ? 1 : 0));
            }

            if (IsSupportedRegistrationType(classSymbol))
            {
                foreach (var ad in cwa.OptionsAttributes)
                {
                    var path = ad.GetConstructorArgument(0).Value as string;
                    if (string.IsNullOrWhiteSpace(path)) continue;
                    options.Add(new OptionsInfo(classSymbol, path!,
                        ad.GetNamedArgument("Name").Value as string,
                        ad.GetNamedArgument("ValidateDataAnnotations").Value is bool data && data,
                        ad.GetNamedArgument("ValidateOnStart").Value is bool start && start,
                        ad.GetNamedArgument("Module").Value as INamedTypeSymbol));
                }
            }
        }

        var assemblyName = compilation.AssemblyName ?? "Generated";
        var sanitized = SanitizeIdentifier(assemblyName);
        var modules = GetModules(compilation, symbols);

        var @namespace = compilation.GetNamespace(analyzerConfigOptionsProvider) ?? "Ling.AutoInject";
        var className = $"{sanitized}_AutoInjectGenerated";
        var methodName = $"Add{sanitized}Services";
        var includeConfiguration = options.Count > 0;

        #region Read configuration

        // Read first AutoInjectExtensionsAttribute from static partial class in assembly
        var foundExtensionsAttribute = false;
        foreach (var tree in compilation.SyntaxTrees)
        {
            var model = compilation.GetSemanticModel(tree);
            var root = tree.GetRoot();
            var classSymbols = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(cds => cds.Modifiers.Any(SyntaxKind.StaticKeyword) && cds.Modifiers.Any(SyntaxKind.PartialKeyword))
                .Select(cd => model.GetDeclaredSymbol(cd))
                .OfType<INamedTypeSymbol>();
            foreach (var classSymbol in classSymbols)
            {
                foreach (var attributeData in classSymbol.GetAttributes())
                {
                    if (!SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, symbols.AutoInjectExtensionsAttributeSymbol))
                    {
                        continue;
                    }

                    var containingNamespace = classSymbol.ContainingNamespace;
                    var namespaceName = containingNamespace.ToDisplayString();
                    @namespace = containingNamespace.IsGlobalNamespace || namespaceName == "<global namespace>"
                        ? string.Empty
                        : namespaceName;
                    className = classSymbol.Name;
                    var methodNameTypedConstant = attributeData.GetNamedArgument("MethodName");
                    if (!methodNameTypedConstant.IsNull
                        && methodNameTypedConstant.ToCSharpString().Trim('"') is string { Length: > 0 } mn)
                    {
                        methodName = mn;
                    }
                    var includeConfigurationTypedConstant = attributeData.GetNamedArgument("IncludeConfiguration");
                    if (!includeConfigurationTypedConstant.IsNull
                        && includeConfigurationTypedConstant.Value is bool ic
                        && ic)
                    {
                        includeConfiguration = true;
                    }

                    foundExtensionsAttribute = true;
                    break; // only one AutoInjectExtensionsAttribute is expected
                }
                if (foundExtensionsAttribute) break;
            }
            if (foundExtensionsAttribute) break;
        }

        if (!foundExtensionsAttribute)
        {
            // Read AutoInjectConfigAttribute from assembly
            var configAttribute = compilation.Assembly.GetAttributes()
                .FirstOrDefault(ad => SymbolEqualityComparer.Default.Equals(ad.AttributeClass, symbols.AutoInjectConfigAttributeSymbol));

            if (configAttribute is not null)
            {
                var namespaceTypedConstant = configAttribute.GetNamedArgument("Namespace");
                if (!namespaceTypedConstant.IsNull
                    && namespaceTypedConstant.ToCSharpString().Trim('"') is string { Length: > 0 } ns)
                {
                    @namespace = ns;
                }

                var classNameTypedConstant = configAttribute.GetNamedArgument("ClassName");
                if (!classNameTypedConstant.IsNull
                    && classNameTypedConstant.ToCSharpString().Trim('"') is string { Length: > 0 } cn)
                {
                    className = cn;
                }

                var methodNameTypedConstant = configAttribute.GetNamedArgument("MethodName");
                if (!methodNameTypedConstant.IsNull
                    && methodNameTypedConstant.ToCSharpString().Trim('"') is string { Length: > 0 } mn)
                {
                    methodName = mn;
                }
            }
        }

        #endregion Read configuration

        var cb = new CodeBuilder();

        cb.AppendLine("// <auto-generated />");
        cb.AppendLine();
        cb.AppendLine("#pragma warning disable");
        cb.AppendLine("#nullable enable annotations");
        cb.AppendLine();
        cb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        cb.AppendLine("using Microsoft.Extensions.DependencyInjection.Extensions;");
        cb.AppendLine();

        var hasNamespace = !string.IsNullOrWhiteSpace(@namespace);
        if (hasNamespace)
        {
            cb.AppendFormatLine("namespace {0}", @namespace);
            cb.OpenBrace();
        }

        cb.AppendLine("/// <summary>");
        cb.AppendLine("/// Auto-generated extension methods for registering services with AutoInject attributes.");
        cb.AppendLine("/// </summary>");
        cb.AppendFormatLine("[global::System.CodeDom.Compiler.GeneratedCode(\"Ling.AutoInject.SourceGenerators\", \"{0}\")]", Constants.Version);
        cb.AppendLine("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
        if (foundExtensionsAttribute)
        {
            cb.AppendFormatLine("static partial class {0}", className);
        }
        else
        {
            cb.AppendFormatLine("public static partial class {0}", className);
        }
        cb.OpenBrace();

        cb.AppendLine("/// <summary>");
        cb.AppendFormatLine("/// Adds services decorated with AutoInject attributes from the assembly '{0}' to the IServiceCollection.", assemblyName);
        cb.AppendLine("/// <para>");
        cb.AppendLine("/// Implements the 'AddAdditionalServices' partial method to further customize service registrations.");
        cb.AppendLine("/// </para>");
        cb.AppendLine("/// </summary>");
        cb.AppendLine("/// <param name=\"services\">The IServiceCollection to add services to.</param>");
        if (includeConfiguration)
        {
            cb.AppendLine("/// <param name=\"configuration\">The configuration.</param>");
        }
        cb.AppendLine("/// <returns>The IServiceCollection for chaining.</returns>");
        if (includeConfiguration)
        {
            cb.AppendFormatLine("public static IServiceCollection {0}(this IServiceCollection services, global::Microsoft.Extensions.Configuration.IConfiguration configuration)", methodName);
        }
        else
        {
            cb.AppendFormatLine("public static IServiceCollection {0}(this IServiceCollection services)", methodName);
        }
        cb.OpenBrace();
        cb.AppendLine("if (services == null) throw new global::System.ArgumentNullException(nameof(services));");
        if (includeConfiguration)
        {
            cb.AppendLine("if (configuration == null) throw new global::System.ArgumentNullException(nameof(configuration));");
        }
        cb.AppendLine();
        cb.AppendLine("AddSingletonServices(services);");
        cb.AppendLine("AddScopedServices(services);");
        cb.AppendLine("AddTransientServices(services);");
        var optionIndex = 0;
        foreach (var option in options.Where(o => o.Module is null).OrderBy(o => o.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).ThenBy(o => o.Name))
        {
            var type = option.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var name = string.IsNullOrEmpty(option.Name) ? "global::Microsoft.Extensions.Options.Options.DefaultName" : $"\"{option.Name!.Replace("\"", "\\\"")}\"";
            var path = option.SectionPath.Replace("\"", "\\\"");
            cb.AppendFormatLine("var autoOptions{0} = services.AddOptions<{1}>({2}).Bind(configuration.GetSection(\"{3}\"));", optionIndex, type, name, path);
            if (option.ValidateDataAnnotations) cb.AppendFormatLine("autoOptions{0}.ValidateDataAnnotations();", optionIndex);
            if (option.ValidateOnStart) cb.AppendFormatLine("autoOptions{0}.ValidateOnStart();", optionIndex);
            optionIndex++;
        }
        foreach (var module in modules.OrderBy(m => m.Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))
        {
            if (options.Any(o => SymbolEqualityComparer.Default.Equals(o.Module, module.Symbol)))
            {
                cb.AppendFormatLine("{0}.{1}(services, configuration);", module.Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), module.MethodName);
            }
            else
            {
                cb.AppendFormatLine("{0}.{1}(services);", module.Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), module.MethodName);
            }
        }
        if (includeConfiguration)
        {
            cb.AppendLine("AddAdditionalServices(services, configuration);");
        }
        else
        {
            cb.AppendLine("AddAdditionalServices(services);");
        }
        cb.AppendLine();
        cb.AppendLine("return services;");
        cb.CloseBrace();
        cb.AppendLine();

        void EmitRegistration(CodeBuilder destination, string lifetime, RegistrationInfo registration, string serviceType, string implementationType, string? providedService, bool selfRegistration)
        {
            var key = registration.ServiceKey;
            var strategy = registration.Strategy;
            if (key is not null)
            {
                var types = $"{serviceType}, {implementationType}";
                var arguments = key;
                if (providedService is not null && strategy != "TryAddEnumerable")
                {
                    types = serviceType;
                    arguments += $", (sp, key) => ({serviceType})sp.GetRequiredKeyedService<{providedService}>(key)";
                }
                var strategyValue = strategy switch
                {
                    "Add" => 0,
                    "Replace" => 2,
                    "TryAddEnumerable" when !selfRegistration => 3,
                    _ => 1,
                };
                destination.AppendFormatLine("global::Ling.AutoInject.AutoInjectKeyedRegistration.Add(services, ServiceDescriptor.Keyed{0}<{1}>({2}), {3});",
                    lifetime, types, arguments, strategyValue);
                return;
            }
            // Enumerable descriptors need a distinguishable implementation type. Self
            // registration has only one implementation, so TryAdd is equivalent here.
            if (strategy == "TryAddEnumerable")
            {
                var descriptor = $"ServiceDescriptor.{lifetime}<{serviceType}, {implementationType}>()";
                destination.AppendFormatLine("services.{0}({1});", selfRegistration ? "TryAdd" : "TryAddEnumerable", descriptor);
                return;
            }
            var isFactory = providedService is not null;

            if (isFactory)
            {
                var factory = $"sp => ({serviceType})sp.GetRequiredService<{providedService}>()";
                switch (strategy)
                {
                    case "Add": destination.AppendFormatLine("services.Add{0}<{1}>({2});", lifetime, serviceType, factory); break;
                    case "Replace": destination.AppendFormatLine("services.Replace(ServiceDescriptor.{0}<{1}>({2}));", lifetime, serviceType, factory); break;
                    default: destination.AppendFormatLine("services.TryAdd{0}<{1}>({2});", lifetime, serviceType, factory); break;
                }
                return;
            }

            var genericTypes = selfRegistration ? implementationType : $"{serviceType}, {implementationType}";
            switch (strategy)
            {
                case "Add": destination.AppendFormatLine("services.Add{0}<{1}>();", lifetime, genericTypes); break;
                case "Replace": destination.AppendFormatLine("services.Replace(ServiceDescriptor.{0}<{1}>());", lifetime, genericTypes); break;
                default: destination.AppendFormatLine("services.TryAdd{0}<{1}>();", lifetime, genericTypes); break;
            }
        }

        // Helper to emit a lifetime-specific private method using CodeBuilder (reduces duplication)
        void EmitLifetimeMethod(CodeBuilder destination, string lifetime, IEnumerable<RegistrationInfo> sourceRegistrations)
        {
            destination.AppendFormatLine("private static void Add{0}Services(IServiceCollection services)", lifetime);
            destination.OpenBrace();

            // Track emitted registrations to avoid duplicates instance resolutions
            var duplicatedServiceDict = new Dictionary<(INamedTypeSymbol Implementation, string? ServiceKey), string>();
            foreach (var reg in sourceRegistrations.Where(r => r.Lifetime == lifetime))
            {
                if (duplicatedServiceDict.TryGetValue((reg.Implementation, reg.ServiceKey), out var providedService))
                {
                    // ServiceType will not be null here because duplicated registrations without ServiceType are impossible
                    if (reg.ServiceType is null)
                    {
                        continue;
                    }

                    var svc = reg.ServiceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    var impl = reg.Implementation.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    EmitRegistration(destination, lifetime, reg, svc, impl, providedService, selfRegistration: false);
                }
                else
                {
                    var impl = reg.Implementation.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    if (reg.ServiceType is null)
                    {
                        EmitRegistration(destination, lifetime, reg, impl, impl, providedService: null, selfRegistration: true);

                        duplicatedServiceDict[(reg.Implementation, reg.ServiceKey)] = impl;
                    }
                    else
                    {
                        var svc = reg.ServiceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        EmitRegistration(destination, lifetime, reg, svc, impl, providedService: null, selfRegistration: false);

                        // Interface registrations may already be supplied or later replaced
                        // by another implementation. Never use them as alias anchors.
                    }
                }
            }

            destination.CloseBrace();
        }

        // emit the three private methods with shared logic
        var unmoduledRegistrations = registrations.Where(r => r.Module is null).ToArray();
        EmitLifetimeMethod(cb, "Singleton", unmoduledRegistrations);
        cb.AppendLine();
        EmitLifetimeMethod(cb, "Scoped", unmoduledRegistrations);
        cb.AppendLine();
        EmitLifetimeMethod(cb, "Transient", unmoduledRegistrations);

        // generate a partial method to allow customization
        cb.AppendLine();
        cb.AppendLine("/// <summary>");
        cb.AppendLine("/// Adds additional services to the container.");
        cb.AppendLine("/// </summary>");
        cb.AppendLine("/// <param name=\"services\">The service collection.</param>");
        if (includeConfiguration)
        {
            cb.AppendLine("/// <param name=\"configuration\">The configuration.</param>");
            cb.AppendLine("static partial void AddAdditionalServices(IServiceCollection services, global::Microsoft.Extensions.Configuration.IConfiguration configuration);");
        }
        else
        {
            cb.AppendLine("static partial void AddAdditionalServices(IServiceCollection services);");
        }

        // close class & namespace
        cb.CloseBrace();
        if (hasNamespace)
        {
            cb.CloseBrace();
        }

        var hintName = $"AutoInject_{sanitized}.g.cs";
        context.AddSourceWithCrlf(hintName, cb.ToString());

        foreach (var module in modules)
        {
            var moduleRegistrations = registrations.Where(r => SymbolEqualityComparer.Default.Equals(r.Module, module.Symbol)).ToArray();
            var moduleOptions = options.Where(o => SymbolEqualityComparer.Default.Equals(o.Module, module.Symbol)).ToArray();
            if (moduleRegistrations.Length == 0 && moduleOptions.Length == 0) continue;

            var moduleCode = new CodeBuilder();
            moduleCode.AppendLine("// <auto-generated />");
            moduleCode.AppendLine();
            moduleCode.AppendLine("#pragma warning disable");
            moduleCode.AppendLine("#nullable enable annotations");
            moduleCode.AppendLine();
            moduleCode.AppendLine("using Microsoft.Extensions.DependencyInjection;");
            moduleCode.AppendLine("using Microsoft.Extensions.DependencyInjection.Extensions;");
            moduleCode.AppendLine();
            var moduleNamespace = module.Symbol.ContainingNamespace.ToDisplayString();
            if (!module.Symbol.ContainingNamespace.IsGlobalNamespace)
            {
                moduleCode.AppendFormatLine("namespace {0}", moduleNamespace);
                moduleCode.OpenBrace();
            }
            moduleCode.AppendFormatLine("static partial class {0}", module.Symbol.Name);
            moduleCode.OpenBrace();
            var moduleNeedsConfiguration = moduleOptions.Length > 0;
            if (moduleNeedsConfiguration)
            {
                moduleCode.AppendFormatLine("public static IServiceCollection {0}(this IServiceCollection services, global::Microsoft.Extensions.Configuration.IConfiguration configuration)", module.MethodName);
            }
            else
            {
                moduleCode.AppendFormatLine("public static IServiceCollection {0}(this IServiceCollection services)", module.MethodName);
            }
            moduleCode.OpenBrace();
            moduleCode.AppendLine("if (services == null) throw new global::System.ArgumentNullException(nameof(services));");
            if (moduleNeedsConfiguration) moduleCode.AppendLine("if (configuration == null) throw new global::System.ArgumentNullException(nameof(configuration));");
            moduleCode.AppendLine("AddSingletonServices(services);");
            moduleCode.AppendLine("AddScopedServices(services);");
            moduleCode.AppendLine("AddTransientServices(services);");
            var moduleOptionIndex = 0;
            foreach (var option in moduleOptions.OrderBy(o => o.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).ThenBy(o => o.Name))
            {
                var type = option.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var name = string.IsNullOrEmpty(option.Name) ? "global::Microsoft.Extensions.Options.Options.DefaultName" : $"\"{option.Name!.Replace("\"", "\\\"")}\"";
                var path = option.SectionPath.Replace("\"", "\\\"");
                moduleCode.AppendFormatLine("var autoOptions{0} = services.AddOptions<{1}>({2}).Bind(configuration.GetSection(\"{3}\"));", moduleOptionIndex, type, name, path);
                if (option.ValidateDataAnnotations) moduleCode.AppendFormatLine("autoOptions{0}.ValidateDataAnnotations();", moduleOptionIndex);
                if (option.ValidateOnStart) moduleCode.AppendFormatLine("autoOptions{0}.ValidateOnStart();", moduleOptionIndex);
                moduleOptionIndex++;
            }
            moduleCode.AppendLine("return services;");
            moduleCode.CloseBrace();
            moduleCode.AppendLine();
            EmitLifetimeMethod(moduleCode, "Singleton", moduleRegistrations);
            moduleCode.AppendLine();
            EmitLifetimeMethod(moduleCode, "Scoped", moduleRegistrations);
            moduleCode.AppendLine();
            EmitLifetimeMethod(moduleCode, "Transient", moduleRegistrations);
            moduleCode.CloseBrace();
            if (!module.Symbol.ContainingNamespace.IsGlobalNamespace) moduleCode.CloseBrace();
            context.AddSourceWithCrlf($"AutoInject_Module_{SanitizeIdentifier(module.Symbol.ToDisplayString())}.g.cs", moduleCode.ToString());
        }
    }

    private static string SanitizeIdentifier(string name)
    {
        var sb = new StringBuilder();
        foreach (var ch in name)
        {
            if (char.IsLetterOrDigit(ch) || ch == '_') sb.Append(ch);
            else sb.Append('_');
        }
        if (sb.Length == 0) sb.Append("A");
        if (char.IsDigit(sb[0])) sb.Insert(0, '_');
        return sb.ToString();
    }

    private static ImmutableArray<ModuleInfo> GetModules(Compilation compilation, AutoInjectSymbols symbols)
    {
        var modules = new Dictionary<INamedTypeSymbol, ModuleInfo>(SymbolEqualityComparer.Default);
        foreach (var tree in compilation.SyntaxTrees)
        {
            var model = compilation.GetSemanticModel(tree);
            foreach (var declaration in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                if (model.GetDeclaredSymbol(declaration) is not INamedTypeSymbol symbol
                    || symbol.ContainingType is not null
                    || !symbol.IsStatic
                    || HasTypeParameters(symbol)
                    || !declaration.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    continue;
                }

                var attribute = symbol.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, symbols.AutoInjectModuleAttributeSymbol));
                if (attribute is null) continue;
                var configuredName = attribute.GetNamedArgument("MethodName").Value as string;
                var methodName = string.IsNullOrWhiteSpace(configuredName) ? $"Add{symbol.Name}" : configuredName!;
                if (!modules.ContainsKey(symbol))
                {
                    modules.Add(symbol, new ModuleInfo(symbol, methodName));
                }
            }
        }

        return modules.Values.ToImmutableArray();
    }

    private static bool IsSupportedRegistrationType(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.TypeKind == TypeKind.Class
            && !typeSymbol.IsAbstract
            && !typeSymbol.IsStatic
            && !HasTypeParameters(typeSymbol);
    }

    private static bool HasTypeParameters(INamedTypeSymbol typeSymbol)
    {
        for (var current = typeSymbol; current is not null; current = current.ContainingType)
        {
            if (current.TypeParameters.Length > 0)
            {
                return true;
            }
        }

        return false;
    }

    private record ClassWithAttributes(INamedTypeSymbol ClassSymbol, ImmutableArray<AttributeData> Attributes, ImmutableArray<AttributeData> OptionsAttributes);
    private record OptionsInfo(INamedTypeSymbol Type, string SectionPath, string? Name, bool ValidateDataAnnotations, bool ValidateOnStart, INamedTypeSymbol? Module);
    private record ModuleInfo(INamedTypeSymbol Symbol, string MethodName);
    private static string? GetLifetime(TypedConstant value)
    {
        return value.Value switch
        {
            0 => "Singleton",
            1 => "Scoped",
            2 => "Transient",
            _ => null,
        };
    }

    private static string GetStrategy(TypedConstant value)
    {
        return value.Value switch
        {
            0 => "Add",
            2 => "Replace",
            3 => "TryAddEnumerable",
            _ => "TryAdd",
        };
    }

    private record RegistrationInfo(INamedTypeSymbol Implementation, string Lifetime, INamedTypeSymbol? ServiceType, string? ServiceKey, string Strategy, INamedTypeSymbol? Module);
}
