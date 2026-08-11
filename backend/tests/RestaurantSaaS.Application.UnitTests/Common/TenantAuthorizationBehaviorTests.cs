using FluentAssertions;
using NSubstitute;
using RestaurantSaaS.Application.Common.Behaviors;
using RestaurantSaaS.Application.Common.Exceptions;
using RestaurantSaaS.Application.Common.Interfaces;
using Xunit;

namespace RestaurantSaaS.Application.UnitTests.Common;

file sealed record FakeTenantRequest(Guid TenantId) : ITenantScopedRequest;

public class TenantAuthorizationBehaviorTests
{
    [Fact]
    public async Task Handle_WhenCallerTenantMatchesRequestTenant_CallsNext()
    {
        var tenantId = Guid.NewGuid();
        var tenantProvider = Substitute.For<ITenantProvider>();
        tenantProvider.TenantId.Returns(tenantId);
        tenantProvider.IsSuperAdmin.Returns(false);

        var behavior = new TenantAuthorizationBehavior<FakeTenantRequest, string>(tenantProvider);
        var called = false;

        var result = await behavior.Handle(new FakeTenantRequest(tenantId), () =>
        {
            called = true;
            return Task.FromResult("ok");
        }, CancellationToken.None);

        called.Should().BeTrue();
        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_WhenCallerTenantDiffersFromRequestTenant_Throws()
    {
        var tenantProvider = Substitute.For<ITenantProvider>();
        tenantProvider.TenantId.Returns(Guid.NewGuid());
        tenantProvider.IsSuperAdmin.Returns(false);

        var behavior = new TenantAuthorizationBehavior<FakeTenantRequest, string>(tenantProvider);

        var act = () => behavior.Handle(new FakeTenantRequest(Guid.NewGuid()), () => Task.FromResult("ok"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_WhenSuperAdmin_BypassesTenantCheck()
    {
        var tenantProvider = Substitute.For<ITenantProvider>();
        tenantProvider.TenantId.Returns((Guid?)null);
        tenantProvider.IsSuperAdmin.Returns(true);

        var behavior = new TenantAuthorizationBehavior<FakeTenantRequest, string>(tenantProvider);

        var result = await behavior.Handle(new FakeTenantRequest(Guid.NewGuid()), () => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
    }
}
