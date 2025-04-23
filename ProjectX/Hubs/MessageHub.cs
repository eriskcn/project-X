using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Models;
using ProjectX.Data;

namespace ProjectX.Hubs;

[Authorize]
public class MessageHub(ApplicationDbContext context) : Hub
{
}