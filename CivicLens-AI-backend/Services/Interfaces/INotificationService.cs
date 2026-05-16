namespace CivicLens.API.Services.Interfaces
{
    /// <summary>
    /// Service responsable de la création des notifications pour les citoyens.
    /// Les méthodes ajoutent l'entité Notification au contexte EF sans sauvegarder —
    /// le SaveChanges est délégué au service appelant (SignalementManagementService)
    /// pour rester dans la même transaction.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Prépare une notification liée à un changement de statut de signalement.
        /// RelatedEvenementId sera null (car c'est une notif de signalement).
        /// </summary>
        void PrepareSignalementNotification(
            Guid userId,
            long signalementId,
            string type,
            string message);

        void PreparePlatformNotification(
            Guid userId,
            string type,
            string message);
    }
}
