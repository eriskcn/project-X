using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.DTOs;
using ProjectX.DTOs.Orders;
using ProjectX.Models;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/orders")]
[Authorize]
public class OrderController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> GetOrder(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized("Invalid user ID.");

        var user = await context.Users.FindAsync(userGuid);
        if (user == null)
            return NotFound(new { Message = "User not found." });

        var order = await context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
        if (order == null)
            return NotFound(new { Message = "Order not found." });

        if (order.UserId != userGuid)
            return Unauthorized(new { Message = "You are not authorized to view this order." });

        OrderResponse response = order.Type switch
        {
            OrderType.TopUp => await BuildTopUpResponse(order),
            OrderType.Job => await BuildJobResponse(order),
            OrderType.Business => await BuildBusinessResponse(order),
            _ => throw new InvalidOperationException("Unknown order type.")
        };

        return Ok(response);
    }

    private async Task<OrderTopUpResponse> BuildTopUpResponse(Order order)
    {
        var topUpTransaction = await context.TokenTransactions.FindAsync(order.TargetId);
        if (topUpTransaction == null)
            throw new Exception("Transaction not found.");

        return new OrderTopUpResponse
        {
            Id = order.Id,
            AmountCash = order.Amount,
            Gateway = order.Gateway,
            Status = order.Status,
            AmountToken = topUpTransaction.AmountToken,
            Created = order.Created,
            Modified = order.Modified
        };
    }

    private async Task<OrderJobResponse> BuildJobResponse(Order order)
    {
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

        return new OrderJobResponse
        {
            Id = order.Id,
            AmountCash = order.Amount,
            Gateway = order.Gateway,
            Status = order.Status,
            Services = jobServices,
            Created = order.Created,
            Modified = order.Modified
        };
    }

    private async Task<OrderBusinessResponse> BuildBusinessResponse(Order order)
    {
        var purchasedPackage = await context.PurchasedPackages
            .Include(pp => pp.BusinessPackage)
            .SingleOrDefaultAsync(pp => pp.Id == order.TargetId);

        if (purchasedPackage == null)
            throw new Exception("Purchased Package not found.");

        return new OrderBusinessResponse
        {
            Id = order.Id,
            AmountCash = order.Amount,
            Gateway = order.Gateway,
            Status = order.Status,
            Created = order.Created,
            Modified = order.Modified,
            PurchasedPackage = new PurchasedPackageResponse
            {
                Id = purchasedPackage.Id,
                BusinessPackage = new BusinessPackageResponse
                {
                    Id = purchasedPackage.BusinessPackage.Id,
                    Name = purchasedPackage.BusinessPackage.Name,
                    Description = purchasedPackage.BusinessPackage.Description,
                    Level = purchasedPackage.BusinessPackage.Level,
                    CashPrice = purchasedPackage.BusinessPackage.CashPrice,
                    DurationInDays = purchasedPackage.BusinessPackage.DurationInDays,
                    MonthlyXTokenRewards = purchasedPackage.BusinessPackage.MonthlyXTokenRewards,
                    Created = purchasedPackage.BusinessPackage.Created,
                    Modified = purchasedPackage.BusinessPackage.Modified
                },
                IsActive = purchasedPackage.IsActive,
                StartDate = purchasedPackage.StartDate,
                EndDate = purchasedPackage.EndDate,
                Created = purchasedPackage.Created,
                Modified = purchasedPackage.Modified
            }
        };
    }
}

public class JobServiceResponse
{
    public Guid Id { set; get; }
    public ServiceType Type { set; get; }
    public bool IsActive { set; get; }
    public DateTime Created { set; get; }
    public DateTime Modified { set; get; }
}

public class PurchasedPackageResponse
{
    public Guid Id { set; get; }
    public BusinessPackageResponse BusinessPackage { set; get; } = null!;
    public bool IsActive { set; get; }
    public DateTime StartDate { set; get; }
    public DateTime EndDate { set; get; }
    public DateTime Created { set; get; }
    public DateTime Modified { set; get; }
}