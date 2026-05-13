export interface Notification {
  id: string;
  type: string;
  message: string;
  relatedEvenementId?: string | null;
  relatedSignalementId?: number | null;
  createdAt: Date;
  isRead: boolean;
}

