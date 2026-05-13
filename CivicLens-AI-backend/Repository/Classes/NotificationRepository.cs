using CivicLens.API.Data;
using CivicLens.API.Models.Entities;
using CivicLens.API.Repository.Interfaces;

namespace CivicLens.API.Repository.Classes
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly AppDbContext _context;

        public NotificationRepository(AppDbContext context)
        {
            _context = context;
        }

        public void Add(Notification notification)
        {
            _context.Notifications.Add(notification);
        }
    }
}
