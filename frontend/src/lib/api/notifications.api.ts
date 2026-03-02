import apiClient from "./client";
import type { ApiResponse } from "@/types";

export interface NotificationDto {
  id: string;
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

  send: (data: { userId?: string; title: string; body: string; type?: string; channels?: string[] }) =>
    apiClient.post<ApiResponse>("/notifications/send", data),
};
