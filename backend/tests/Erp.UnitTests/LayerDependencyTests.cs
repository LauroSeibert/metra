using System.Reflection;
using Erp.Infrastructure.Persistence;

namespace Erp.UnitTests;

// Garante as regras de dependência entre camadas descritas em docs/architecture.md.
public sealed class LayerDependencyTests
{
    private static readonly Assembly DomainAssembly = Assembly.Load("Erp.Domain");
    private static readonly Assembly ApplicationAssembly = Assembly.Load("Erp.Application");
    private static readonly Assembly InfrastructureAssembly = typeof(ErpDbContext).Assembly;

    [Fact]
    public void Domain_DoesNotReference_OtherProjectsOrInfrastructurePackages()
    {
        var references = ReferencedNames(DomainAssembly);

        Assert.DoesNotContain(references, name => name.StartsWith("Erp.", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name.StartsWith("MySql", StringComparison.Ordinal));
    }

    [Fact]
    public void Application_References_OnlyDomain()
    {
        var erpReferences = ReferencedNames(ApplicationAssembly)
            .Where(name => name.StartsWith("Erp.", StringComparison.Ordinal));

        Assert.All(erpReferences, name => Assert.Equal("Erp.Domain", name));
    }

    [Fact]
    public void Infrastructure_DoesNotReference_Api()
    {
        Assert.DoesNotContain("Erp.Api", ReferencedNames(InfrastructureAssembly));
    }

    private static string[] ReferencedNames(Assembly assembly) =>
        [.. assembly.GetReferencedAssemblies().Select(reference => reference.Name ?? string.Empty)];
}
