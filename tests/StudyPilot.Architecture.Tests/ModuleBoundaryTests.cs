using System.Reflection;
using StudyPilot.Infrastructure.Modules;
using StudyPilot.Modules.Academic;
using StudyPilot.Modules.Identity;
using StudyPilot.SharedKernel.Domain;

namespace StudyPilot.Architecture.Tests;

/// <summary>
/// Enforces the module boundaries described in ADR-001 (modular monolith). These rules are the
/// difference between a modular monolith and a big ball of mud with folders, so they are asserted
/// in CI rather than left to review.
/// </summary>
public class ModuleBoundaryTests
{
    private static readonly Assembly SharedKernel = typeof(IAggregateRoot).Assembly;
    private static readonly Assembly Infrastructure = typeof(IModule).Assembly;
    private static readonly Assembly IdentityModuleAssembly = typeof(IdentityModule).Assembly;
    private static readonly Assembly AcademicModuleAssembly = typeof(AcademicModule).Assembly;

    private static readonly Assembly[] ModuleAssemblies =
        [IdentityModuleAssembly, AcademicModuleAssembly];

    private static string[] ReferencedNames(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToArray();

    [Fact]
    public void Modules_do_not_reference_one_another()
    {
        foreach (var module in ModuleAssemblies)
        {
            var others = ModuleAssemblies
                .Where(other => other != module)
                .Select(other => other.GetName().Name!);

            var referenced = ReferencedNames(module);

            foreach (var other in others)
            {
                Assert.DoesNotContain(other, referenced);
            }
        }
    }

    [Fact]
    public void Shared_infrastructure_does_not_reference_any_module()
    {
        var referenced = ReferencedNames(Infrastructure);

        foreach (var module in ModuleAssemblies)
        {
            Assert.DoesNotContain(module.GetName().Name!, referenced);
        }
    }

    [Fact]
    public void Shared_kernel_depends_on_neither_infrastructure_nor_modules()
    {
        var referenced = ReferencedNames(SharedKernel);

        Assert.DoesNotContain(Infrastructure.GetName().Name!, referenced);

        foreach (var module in ModuleAssemblies)
        {
            Assert.DoesNotContain(module.GetName().Name!, referenced);
        }
    }

    [Fact]
    public void Shared_infrastructure_contains_no_domain_entities()
    {
        // "No domain-specific business logic in shared infrastructure" (SP-140 AC3), checked by
        // the concrete proxy that no domain entity or aggregate root is declared there.
        var domainTypes = Infrastructure.GetTypes()
            .Where(t => typeof(IAggregateRoot).IsAssignableFrom(t)
                        || (t.BaseType?.IsGenericType == true
                            && t.BaseType.GetGenericTypeDefinition() == typeof(Entity<>)))
            .Select(t => t.FullName)
            .ToArray();

        Assert.Empty(domainTypes);
    }

    [Fact]
    public void Every_module_type_is_sealed_and_has_a_parameterless_constructor()
    {
        // The host composes modules with `new`, so each must be constructible without a container.
        foreach (var assembly in ModuleAssemblies)
        {
            var moduleTypes = assembly.GetTypes()
                .Where(t => typeof(IModule).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false })
                .ToArray();

            Assert.NotEmpty(moduleTypes);

            foreach (var type in moduleTypes)
            {
                Assert.True(type.IsSealed, $"{type.FullName} should be sealed.");
                Assert.NotNull(type.GetConstructor(Type.EmptyTypes));
            }
        }
    }
}
