using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Ahk.GitHub.Monitor.Tests.UnitTests.EventHandlersTests.Helpers;

public static class ServiceProviderMock
{
    public static IServiceProvider GetMockedObject()
    {
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(ILogger<GitHubMonitorFunction>)))
            .Returns(NullLogger<GitHubMonitorFunction>.Instance);
        return serviceProviderMock.Object;
    }
}
