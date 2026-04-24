using DomainViewer.API.Common;
using DomainViewer.API.Controllers;
using DomainViewer.Core.Entities;
using DomainViewer.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DomainViewer.Tests;

public class DashboardControllerTests
{
    private ApplicationDbContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetStats_ReturnsCorrectCounts()
    {
        var context = GetInMemoryContext();
        context.Domains.AddRange(
            new Domain { Name = "active1.com", ExpirationDate = DateTime.UtcNow.AddDays(60), IsActive = true },
            new Domain { Name = "active2.com", ExpirationDate = DateTime.UtcNow.AddDays(10), IsActive = true },
            new Domain { Name = "expired.com", ExpirationDate = DateTime.UtcNow.AddDays(-5), IsActive = true },
            new Domain { Name = "inactive.com", ExpirationDate = DateTime.UtcNow.AddDays(10), IsActive = false }
        );
        context.Users.AddRange(
            new User { Email = "a@test.com", Name = "A", Provider = "local", ExternalId = "1", IsActive = true },
            new User { Email = "b@test.com", Name = "B", Provider = "local", ExternalId = "2", IsActive = true },
            new User { Email = "c@test.com", Name = "C", Provider = "local", ExternalId = "3", IsActive = false }
        );
        await context.SaveChangesAsync();

        var controller = new DashboardController(context);
        var result = await controller.GetStats();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<DashboardStatsResponse>>(okResult.Value);
        var stats = apiResponse.Data;
        Assert.Equal(3, stats.TotalDomains);
        Assert.Equal(1, stats.ExpiredDomains);
        Assert.Equal(1, stats.UpcomingDomains); // only active2.com (10 days)
        Assert.Equal(2, stats.TotalUsers);
    }

    [Fact]
    public async Task GetUpcomingDomains_ReturnsOrderedList()
    {
        var context = GetInMemoryContext();
        context.Domains.AddRange(
            new Domain { Name = "later.com", ExpirationDate = DateTime.UtcNow.AddDays(25), IsActive = true },
            new Domain { Name = "sooner.com", ExpirationDate = DateTime.UtcNow.AddDays(5), IsActive = true },
            new Domain { Name = "expired.com", ExpirationDate = DateTime.UtcNow.AddDays(-5), IsActive = true }
        );
        await context.SaveChangesAsync();

        var controller = new DashboardController(context);
        var result = await controller.GetUpcomingDomains();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<List<UpcomingDomainResponse>>>(okResult.Value);
        var list = apiResponse.Data;
        Assert.Equal(2, list.Count);
        Assert.Equal("sooner.com", list[0].Name);
        Assert.Equal("later.com", list[1].Name);
        Assert.True(list[0].DaysUntilExpiration < list[1].DaysUntilExpiration);
    }
}
