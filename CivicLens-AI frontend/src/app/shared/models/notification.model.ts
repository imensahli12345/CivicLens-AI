export interface Notification {
  id: string;
  type: string;
  message: string;
  relatedEvenementId?: string | null;
  createdAt: Date;
  isRead: boolean;
}

