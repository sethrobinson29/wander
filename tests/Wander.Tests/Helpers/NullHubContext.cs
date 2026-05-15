using Microsoft.AspNetCore.SignalR;

namespace Wander.Tests.Helpers;

public sealed class NullHubContext<T> : IHubContext<T> where T : Hub
{
    public static readonly NullHubContext<T> Instance = new();
    public IHubClients Clients => null!;
    public IGroupManager Groups => null!;
}
