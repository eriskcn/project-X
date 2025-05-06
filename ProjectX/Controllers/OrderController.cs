using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.DTOs;
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

    [HttpGet("business/{id:guid}")]
    public async Task<ActionResult<OrderBusinessResponse>> GetBusinessOrder(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized(new { Message = "Invalid user Id" });
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

        if (userGuid != order.UserId)
        {
            return Unauthorized(new { Message = "You are not authorized to view this order." });
        }

        var purchasedPackage = await context.PurchasedPackages
            .Include(pp => pp.BusinessPackage)
            .SingleOrDefaultAsync(pp => pp.Id == order.TargetId);

        if (purchasedPackage == null)
        {
            return NotFound(new { Message = "Purchased Package not found." });
        }


        var response = new OrderBusinessResponse
        {
            Id = order.Id,
            Amount = order.Amount,
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
            },
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

public class OrderBusinessResponse
{
    public Guid Id { set; get; }
    public double Amount { set; get; }
    public PurchasedPackageResponse PurchasedPackage { set; get; } = null!;
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