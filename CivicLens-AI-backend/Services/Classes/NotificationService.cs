using CivicLens.API.Models.Entities;
using CivicLens.API.Repository.Interfaces;
using CivicLens.API.Services.Interfaces;

namespace CivicLens.API.Services.Classes
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;

        public NotificationService(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        /// <inheritdoc/>
        public void PrepareSignalementNotification(
            Guid citoyenId,
            long signalementId,
            string type,
            string message)
        {
            var notification = new Notification
            {
                CitoyenId = citoyenId,
                Type = type,
                Message = message,
                // RelatedEvenementId is intentionally null — this notification concerns a Signalement
                RelatedSignalementId = signalementId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _notificationRepository.Add(notification);
        }
    }
}
