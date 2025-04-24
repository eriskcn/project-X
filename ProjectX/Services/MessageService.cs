using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.DTOs;
using ProjectX.Models;

namespace ProjectX.Services
{
    public class MessageService(ApplicationDbContext context) : IMessageService
    {
        public async Task<Message> CreateMessageAsync(Guid senderId, Guid receiverId, string content)
        {
            var conversation = await FindOrCreateConversationAsync(senderId, receiverId);

            var message = new Message
            {
                SenderId = senderId,
                ConversationId = conversation.Id,
                Content = content,
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow
            };

            context.Messages.Add(message);
            await context.SaveChangesAsync();

            return message;
        }

        private async Task<Conversation> FindOrCreateConversationAsync(Guid user1Id, Guid user2Id)
        {
            var user1 = await context.Users.FindAsync(user1Id);
            var user2 = await context.Users.FindAsync(user2Id);
            if (user1 == null)
            {
                return null;
            }

            if (user2 == null)
            {
                return null;
            }

            var conversation = await context.Conversations
                .Include(c => c.Participants)
                .SingleOrDefaultAsync(c => c.Participants.Contains(user1) && c.Participants.Contains(user2));

            if (conversation == null)
            {
                return new Conversation
                {
                    
                };
            }

            return new Conversation();
        }
    }
}