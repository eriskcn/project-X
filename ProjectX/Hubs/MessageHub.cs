using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Models;

namespace ProjectX.Hubs;

[Authorize]
public class MessageHub : Hub
{
}