using CommunityStarter.Api.Infrastructure;
using CommunityStarter.Application.Abstractions;
using CommunityStarter.Domain.Communities;
using CommunityStarter.Domain.Common;
using Microsoft.AspNetCore.SignalR;

namespace CommunityStarter.Api.Realtime;

public sealed class CommunityHub(IPlatformStore store) : Hub
{
    public override Task OnConnectedAsync()
    {
        HttpContext context = Context.GetHttpContext()
            ?? throw new DomainException("authentication_required", "Sign in is required.");
        _ = context.RequireCurrentAccount();
        return base.OnConnectedAsync();
    }

    public async Task SubscribeToCommunity(Guid communityId)
    {
        HttpContext context = Context.GetHttpContext()
            ?? throw new DomainException("authentication_required", "Sign in is required.");
        Application.Contracts.CurrentAccount account = context.RequireCurrentAccount();
        Membership? membership = await store.FindMembershipAsync(communityId, account.Id, Context.ConnectionAborted);
        if (membership?.Status != MembershipStatus.Active)
        {
            throw new DomainException("permission_denied", "An active community membership is required.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"community:{communityId:D}");
    }
}
