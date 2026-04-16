import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { Notification } from '../../shared/models/notification.model';
import { map } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  constructor(private api: ApiService) {}

  getMyNotifications(limit: number): Observable<Notification[]> {
    return this.api.get<Notification[]>(`/notifications/mes?limit=${limit}`);
  }

  getMyUnreadCount(): Observable<number> {
    return this.api.get<{ unreadCount: number }>(`/notifications/mes/unread-count`).pipe(
      map(res => res.unreadCount)
    );
  }

  getMyAllNotifications(): Observable<Notification[]> {
    return this.api.get<Notification[]>(`/notifications/mes`);
  }

  markAsRead(id: string): Observable<any> {
    return this.api.put<any>(`/notifications/${id}/read`, {});
  }

  markAllAsRead(): Observable<any> {
    return this.api.put<any>(`/notifications/mes/mark-all-read`, {});
  }
}

