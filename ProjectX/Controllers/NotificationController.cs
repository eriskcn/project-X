using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.DTOs;
using ProjectX.Models;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/notifications")]
[Authorize(Policy = "EmailConfirmed")]
public class NotificationController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationResponse>>> GetOwnNotifications([FromQuery] bool? isRead)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return NotFound(new { Message = "User ID not found in access token." });
        }

        var user = await context.Users.FindAsync(Guid.Parse(userId));
        if (user == null)
        {
            return NotFound(new { Message = "User not found." });
        }

        var notifications = await context.Notifications
            .Where(n => n.RecipientId == Guid.Parse(userId) && n.Type != NotificationType.SuccessPayment &&
                        (!isRead.HasValue || n.IsRead == isRead))
            .OrderByDescending(n => n.Created)
            .Select(n => new NotificationResponse
            {
                Id = n.Id,
                Type = n.Type,
                RecipientId = n.RecipientId,
                TargetId = n.TargetId,
                IsRead = n.IsRead,
                Read = n.Read,
                Created = n.Created
            })
            .ToListAsync();

        return Ok(notifications);
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkNotificationAsRead(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return NotFound(new { Message = "User ID not found in access token." });
        }

        var notification = await context.Notifications
            .SingleOrDefaultAsync(n => n.Id == id && n.RecipientId == Guid.Parse(userId));
        if (notification == null)
        {
            return NotFound(new { Message = "Notification not found." });
        }

        notification.IsRead = true;
        notification.Read = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return Ok(new { Message = "Notification marked as read." });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteNotification(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return NotFound(new { Message = "User ID not found in access token." });
        }

        var notification = await context.Notifications
            .SingleOrDefaultAsync(n => n.Id == id && n.RecipientId == Guid.Parse(userId));
        if (notification == null)
        {
            return NotFound(new { Message = "Notification not found." });
        }

        context.Notifications.Remove(notification);
        await context.SaveChangesAsync();

        return Ok(new { Message = "Notification deleted." });
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllNotificationsAsRead()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return NotFound(new { Message = "User ID not found in access token." });
        }

        var notifications = await context.Notifications
            .Where(n => n.RecipientId == Guid.Parse(userId) && !n.IsRead)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.Read = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();

        return Ok(new { Message = "All notifications marked as read." });
    }
}