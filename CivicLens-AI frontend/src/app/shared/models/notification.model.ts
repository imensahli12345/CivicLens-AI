export interface Notification {
  id: string;
  type: string;
  message: string;
  relatedEvenementId?: string | null;
  relatedSignalementId?: number | null;
  relatedReclamationId?: string | null;
  relatedUserId?: string | null;
  createdAt: Date;
  isRead: boolean;
}
