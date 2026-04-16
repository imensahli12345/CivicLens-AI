using CivicLens.API.Models.Entities;

namespace CivicLens.API.Repository.Interfaces
{
    /// <summary>
    /// Repository pour la persistance des notifications.
    /// La méthode Add() ne sauvegarde PAS immédiatement — le caller contrôle le SaveChanges()
    /// afin que la notification soit incluse dans la même transaction que le changement de statut.
    /// </summary>
    public interface INotificationRepository
    {
        /// <summary>Ajoute la notification au contexte EF Core (sans SaveChanges).</summary>
        void Add(Notification notification);
    }
}
