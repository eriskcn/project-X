using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.Models;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/orders")]
[Authorize]
public class OrderController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet("top-up/{id:guid}")]
    public async Task<ActionResult<OrderTopUpResponse>> GetTopUpOrder(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized("Invalid user ID.");
        }

        var user = await context.Users.FindAsync(userGuid);
        if (user == null)
        {
            return NotFound(new { Message = "User not found." });
        }

        var order = await context.Orders.FindAsync(id);
        if (order == null)
        {
            return NotFound(new { Message = "Order not found." });
        }

        if (order.UserId != userGuid)
        {
            return Unauthorized(new { Message = "You are not authorized to view this order." });
        }

        var topUpTransaction = await context.TokenTransactions.FindAsync(order.TargetId);
        if (topUpTransaction == null)
        {
            return NotFound(new { Message = "Transaction not found." });
        }

        var response = new OrderTopUpResponse
        {
            Id = order.Id,
            AmountCash = order.Amount,
            AmountToken = topUpTransaction.AmountToken,
            Gateway = order.Gateway,
            Created = order.Created,
            Modified = order.Modified
        };
        return Ok(response);
    }

    [HttpGet("job/{id:guid}")]
    public async Task<ActionResult<OrderJobResponse>> GetJobOrder(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized("Invalid user ID.");
        }

        var user = await context.Users.FindAsync(userGuid);
        if (user == null)
        {
            return NotFound(new { Message = "User not found." });
        }

        var order = await context.Orders.FindAsync(id);
        if (order == null)
        {
            return NotFound(new { Message = "Order not found." });
        }

        if (order.UserId != userGuid)
        {
            return Unauthorized(new { Message = "You are not authorized to view this order." });
        }

        var jobServices = await context.JobServices
            .Include(js => js.Service)
            .Where(js => js.JobId == order.TargetId)
            .Select(js => new JobServiceResponse
            {
                Id = js.Id,
                Type = js.Service.Type,
                IsActive = js.IsActive,
                Created = js.Created,
                Modified = js.Modified
            })
            .ToListAsync();

        var response = new OrderJobResponse
        {
            Id = order.Id,
            AmountCash = order.Amount,
            Gateway = order.Gateway,
            Services = jobServices,
            Created = order.Created,
            Modified = order.Modified
        };

        return Ok(response);
    }
}

public class OrderJobResponse
{
    public Guid Id { get; set; }
    public double AmountCash { get; set; }
    public PaymentGateway Gateway { get; set; }
    public ICollection<JobServiceResponse>? Services { get; set; } = new List<JobServiceResponse>();
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
}

public class JobServiceResponse
{
    public Guid Id { set; get; }
    public ServiceType Type { set; get; }
    public bool IsActive { set; get; }
    public DateTime Created { set; get; }
    public DateTime Modified { set; get; }
}