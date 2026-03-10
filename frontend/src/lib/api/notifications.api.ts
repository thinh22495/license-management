import apiClient from "./client";
import type { ApiResponse, PagedResult } from "@/types";

export interface NotificationDto {
  id: string;
  title: string;
  body: string;
  type: string;
  isRead: boolean;
  createdAt: string;
}

export interface AdminNotificationDto {
  id: string;
  userId: string | null;
  userEmail: string | null;
  userFullName: string | null;
  title: string;
  body: string;
  type: string;
  isRead: boolean;
  createdAt: string;
}

export const notificationsApi = {
  getAll: (params?: { unreadOnly?: boolean; limit?: number }) =>
    apiClient.get<ApiResponse<NotificationDto[]>>("/notifications", { params }),

  getUnreadCount: () =>
    apiClient.get<ApiResponse<number>>("/notifications/unread-count"),

  markRead: (notificationId?: string) =>
    apiClient.put<ApiResponse>("/notifications/read", { notificationId }),

  delete: (notificationId: string) =>
    apiClient.delete<ApiResponse>(`/notifications/${notificationId}`),

  send: (data: { userId?: string; title: string; body: string; type?: string; channels?: string[] }) =>
    apiClient.post<ApiResponse>("/notifications/send", data),

  adminGetAll: (params?: { page?: number; pageSize?: number; search?: string; type?: string }) =>
    apiClient.get<ApiResponse<PagedResult<AdminNotificationDto>>>("/notifications/all", { params }),

  adminDelete: (notificationId: string) =>
    apiClient.delete<ApiResponse>(`/notifications/admin/${notificationId}`),
};
