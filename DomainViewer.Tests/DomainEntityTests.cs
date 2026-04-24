using DomainViewer.Core.Entities;
using DomainViewer.Core.Enums;

namespace DomainViewer.Tests;

public class DomainEntityTests
{
    [Fact]
    public void User_DefaultRole_IsEmployee()
    {
        var user = new User();
        Assert.Equal(UserRole.Employee, user.Role);
    }

    [Fact]
    public void User_IsActive_DefaultsToTrue()
    {
        var user = new User();
        Assert.True(user.IsActive);
    }

    [Fact]
    public void Domain_IsActive_DefaultsToTrue()
    {
        var domain = new Domain();
        Assert.True(domain.IsActive);
    }

    [Fact]
    public void NotificationLog_Status_DefaultsToPending()
    {
        var log = new NotificationLog();
        Assert.Equal(NotificationStatus.Pending, log.Status);
    }
}
