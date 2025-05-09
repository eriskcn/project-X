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
[Authorize(Policy = "EmailConfirmed")]
public class OrderController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetOwnOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page <= 0 || pageSize < 0)
        {
            return BadRequest(new { Message = "Invalid page or pageSize." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized(new { Message = "Invalid user Id." });
        }

        var user = await context.Users.FindAsync(userGuid);
        if (user == null)
        {
            return NotFound(new { Message = "User not found." });
        }

        var baseQuery = context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userGuid);

        var orderedQuery = baseQuery.OrderByDescending(o => o.Created);

        var totalItems = await orderedQuery.CountAsync();

        if (pageSize > 0)
        {
            pageSize = Math.Min(pageSize, 100);
            orderedQuery = (IOrderedQueryable<Order>)orderedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize);
        }

        var orders = await orderedQuery.ToListAsync();

        var responses = new List<OrderResponse>();
        foreach (var order in orders)
        {
            OrderResponse response = order.Type switch
            {
                OrderType.TopUp => await BuildTopUpResponse(order),
                OrderType.Job => await BuildJobResponse(order),
                OrderType.Business => await BuildBusinessResponse(order),
                _ => throw new InvalidOperationException("Unknown order type.")
            };
            responses.Add(response);
        }

        return Ok(new
        {
            Items = responses,
            TotalItems = totalItems,
            First = page == 1,
            Last = pageSize == 0 || page * pageSize >= totalItems,
            PageNumber = page,
            PageSize = pageSize
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> GetOrder(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized("Invalid user ID.");

        var user = await context.Users.FindAsync(userGuid);
        if (user == null)
            return NotFound(new { Message = "User not found." });

        var order = await context.Orders
            .AsNoTracking()
            .SingleOrDefaultAsync(o => o.Id == id);
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