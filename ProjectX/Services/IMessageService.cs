using ProjectX.Models;

namespace ProjectX.Services;

public interface IMessageService
{
    Task<Message> CreateMessageAsync(Guid senderId, Guid receiverId, string content);
}