using Ling.AutoInject.SourceGenerators.Extensions;
using Ling.AutoInject.SourceGenerators.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
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
            predicate: static (node, _) => node is ClassDeclarationSyntax cds && cds.AttributeLists.Count > 0,
            transform: static (ctx, _) =>
            {
                var classDecl = (ClassDeclarationSyntax)ctx.Node;
                var model = ctx.SemanticModel;
                if (model.GetDeclaredSymbol(classDecl) is INamedTypeSymbol namedTypeSymbol)
                {
                    var symbols = new AutoInjectSymbols(model.Compilation);
                    var attrs = namedTypeSymbol.GetAttributes()
                        .Where(ad => symbols.IsAutoInjectAttribute(ad.AttributeClass))
                        .ToImmutableArray();
                    if (attrs.Length > 0)
                    {
                        return new ClassWithAttributes(namedTypeSymbol, attrs);
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
        context.AddSource("AutoInjectConfigAttribute.g.cs", SourceCodes.AutoInjectConfigAttribute);
        context.AddSource("SingletonServiceAttribute.g.cs", SourceCodes.SingletonServiceAttribute);
        context.AddSource("ScopedServiceAttribute.g.cs", SourceCodes.ScopedServiceAttribute);
        context.AddSource("TransientServiceAttribute.g.cs", SourceCodes.TransientServiceAttribute);
    }

    private void Execute(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<ClassWithAttributes> classes,
        AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider)
    {
        var registrations = new List<RegistrationInfo>();
        var symbols = new AutoInjectSymbols(compilation);

        foreach (var cwa in classes)
        {
            var classSymbol = cwa.ClassSymbol;
            var attrData = cwa.Attributes;

            var regList = new List<RegistrationRecord>();

            foreach (var ad in attrData)
            {
                var lifetime = symbols.GetLifetime(ad.AttributeClass);
                if (lifetime is null)
                {
                    return;
                }

                var serviceTypedConstant = ad.GetConstructorArgument(0);
                if (serviceTypedConstant.IsNull)
                {
                    serviceTypedConstant = ad.GetNamedArgument("ServiceType");
                }
                var serviceKeyTypedConstant = ad.GetNamedArgument("ServiceKey");

                var serviceTypeSymbol = serviceTypedConstant.Value as INamedTypeSymbol;
                var serviceKeyLiteral = serviceKeyTypedConstant.IsNull ? null : serviceKeyTypedConstant.ToCSharpString();

                regList.Add(new RegistrationRecord(lifetime, serviceTypeSymbol, ad, serviceKeyLiteral));
            }

            foreach (var rec in regList.OrderBy(r => r.ServiceType is null ? 0 : 1))
            {
                registrations.Add(new RegistrationInfo(classSymbol, rec.ServiceType, rec.Lifetime, rec.ServiceKeyLiteral));
            }
        }

        var targetVerison = compilation.FindReferenceAssemblyVersionByTypeMetadataName(Constants.ServiceCollectionServiceExtensionsFullName);
        var supportKeyedService = targetVerison > Constants.SupportKeyedServiceVersion;

        var assemblyName = compilation.AssemblyName ?? "Generated";
        var sanitized = SanitizeIdentifier(assemblyName);

        var @namespace = compilation.GetNamespace(analyzerConfigOptionsProvider) ?? "Ling.AutoInject";
        var className = $"{sanitized}_AutoInjectGenerated";
        var methodName = $"Add{sanitized}Services";

        foreach (var attributeData in compilation.Assembly.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, symbols.AutoInjectConfigAttributeSymbol))
            {
                foreach (var arg in attributeData.NamedArguments)
                {
                    switch (arg.Key)
                    {
                        case "Namespace":
                            if (!arg.Value.IsNull)
                            {
                                var ns = arg.Value.ToCSharpString().Trim('"');
                                if (!string.IsNullOrWhiteSpace(ns))
                                {
                                    @namespace = ns;
                                }
                            }
                            break;

                        case "ClassName":
                            if (!arg.Value.IsNull)
                            {
                                var cn = arg.Value.ToCSharpString().Trim('"');
                                if (!string.IsNullOrWhiteSpace(cn))
                                {
                                    className = cn;
                                }
                            }
                            break;

                        case "MethodName":
                            if (!arg.Value.IsNull)
                            {
                                var mn = arg.Value.ToCSharpString().Trim('"');
                                if (!string.IsNullOrWhiteSpace(mn))
                                {
                                    methodName = mn;
                                }
                            }
                            break;

                        default:
                            break;
                    }
                }
            }
        }

        var cb = new CodeBuilder();

        cb.AppendLine("// <auto-generated />");
        cb.AppendLine();
        cb.AppendLine("#pragma warning disable");
        cb.AppendLine("#nullable enable annotations");
        cb.AppendLine();
        cb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        cb.AppendLine("using Microsoft.Extensions.DependencyInjection.Extensions;");
        cb.AppendLine();

        cb.AppendFormatLine("namespace {0}", @namespace);
        cb.OpenBrace();

        cb.AppendLine("/// <summary>");
        cb.AppendLine("/// Auto-generated extension methods for registering services with AutoInject attributes.");
        cb.AppendLine("/// </summary>");
        cb.AppendFormatLine("[global::System.CodeDom.Compiler.GeneratedCode(\"Ling.AutoInject.SourceGenerators\", \"{0}\")]", Constants.Version);
        cb.AppendLine("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
        cb.AppendFormatLine("public static class {0}", className);
        cb.OpenBrace();

        cb.AppendLine("/// <summary>");
        cb.AppendFormatLine("/// Adds services decorated with AutoInject attributes from the assembly '{0}' to the IServiceCollection.", assemblyName);
        cb.AppendLine("/// </summary>");
        cb.AppendLine("/// <param name=\"services\">The IServiceCollection to add services to.</param>");
        cb.AppendLine("/// <returns>The IServiceCollection for chaining.</returns>");
        cb.AppendFormatLine("public static IServiceCollection {0}(this IServiceCollection services)", methodName);
        cb.OpenBrace();
        cb.AppendLine("if (services == null) throw new ArgumentNullException(nameof(services));");
        cb.AppendLine();
        cb.AppendLine("AddSingletonServices(services);");
        cb.AppendLine("AddScopedServices(services);");
        cb.AppendLine("AddTransientServices(services);");
        cb.AppendLine();
        cb.AppendLine("return services;");
        cb.CloseBrace();
        cb.AppendLine();

        // helper to emit a lifetime-specific private method using CodeBuilder (reduces duplication)
        void EmitLifetimeMethod(string lifetime)
        {
            cb.AppendFormatLine("private static void Add{0}Services(IServiceCollection services)", lifetime);
            cb.OpenBrace();

            // track emitted registrations to avoid duplicates
            var dict = new Dictionary<(INamedTypeSymbol Implementation, string? ServiceKey), string>();
            foreach (var reg in registrations.Where(r => r.Lifetime == lifetime))
            {
                var serviceKey = supportKeyedService ? reg.ServiceKeyLiteral : null;
                if (dict.TryGetValue((reg.Implementation, serviceKey), out var exp))
                {
                    if (serviceKey is null)
                    {
                        if (reg.ServiceType is null)
                        {
                            cb.AppendFormatLine("services.TryAdd{0}<{1}>(sp => sp.GetRequiredService<{1}>());", lifetime, exp);
                        }
                        else
                        {
                            var svc = reg.ServiceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            cb.AppendFormatLine("services.TryAdd{0}<{1}>(sp => ({1})sp.GetRequiredService<{2}>());", lifetime, svc, exp);
                        }
                    }
                    else
                    {
                        if (reg.ServiceType is null)
                        {
                            cb.AppendFormatLine("services.TryAddKeyed{0}<{1}>({2}, (sp, key) => sp.GetRequiredKeyedService<{1}>(key));", lifetime, exp, serviceKey);
                        }
                        else
                        {
                            var svc = reg.ServiceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            cb.AppendFormatLine("services.TryAddKeyed{0}<{1}>({2}, (sp, key) => ({1})sp.GetRequiredKeyedService<{3}>(key));", lifetime, svc, serviceKey, exp);
                        }
                    }
                }
                else
                {
                    var impl = reg.Implementation.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    if (serviceKey is null)
                    {
                        if (reg.ServiceType is null)
                        {
                            cb.AppendFormatLine("services.TryAdd{0}<{1}>();", lifetime, impl);
                            dict[(reg.Implementation, null)] = impl;
                        }
                        else
                        {
                            var svc = reg.ServiceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            cb.AppendFormatLine("services.TryAdd{0}<{1}, {2}>();", lifetime, svc, impl);
                            dict[(reg.Implementation, null)] = svc;
                        }
                    }
                    else
                    {
                        if (reg.ServiceType is null)
                        {
                            cb.AppendFormatLine("services.TryAddKeyed{0}<{1}>({2});", lifetime, impl, serviceKey);
                            dict[(reg.Implementation, serviceKey)] = impl;
                        }
                        else
                        {
                            var svc = reg.ServiceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            cb.AppendFormatLine("services.TryAddKeyed{0}<{1}, {2}>({3});", lifetime, svc, impl, serviceKey);
                            dict[(reg.Implementation, serviceKey)] = svc;
                        }
                    }
                }
            }

            cb.CloseBrace();
        }

        // emit the three private methods with shared logic
        EmitLifetimeMethod("Singleton");
        cb.AppendLine();
        EmitLifetimeMethod("Scoped");
        cb.AppendLine();
        EmitLifetimeMethod("Transient");

        // close class & namespace
        cb.CloseBrace();
        cb.CloseBrace();

        var hintName = $"AutoInject_{sanitized}.g.cs";
        context.AddSource(hintName, SourceText.From(cb.ToString(), Encoding.UTF8));
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

    private record ClassWithAttributes(INamedTypeSymbol ClassSymbol, ImmutableArray<AttributeData> Attributes);
    private record RegistrationRecord(string Lifetime, INamedTypeSymbol? ServiceType, AttributeData AttributeData, string? ServiceKeyLiteral);
    private record RegistrationInfo(INamedTypeSymbol Implementation, INamedTypeSymbol? ServiceType, string Lifetime, string? ServiceKeyLiteral);
}
