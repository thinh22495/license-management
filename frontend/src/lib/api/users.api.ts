import apiClient from "./client";
import type { ApiResponse, UserDto, PagedResult } from "@/types";

export const usersApi = {
  getAll: (params?: { page?: number; pageSize?: number; search?: string; role?: string }) =>
    apiClient.get<ApiResponse<PagedResult<UserDto>>>("/users", { params }),

  getById: (id: string) =>
    apiClient.get<ApiResponse<UserDto>>(`/users/${id}`),

  update: (id: string, data: { fullName?: string; phone?: string }) =>
    apiClient.put<ApiResponse<UserDto>>(`/users/${id}`, data),

  lock: (id: string, isLocked: boolean) =>
    apiClient.put<ApiResponse>(`/users/${id}/lock`, { isLocked }),
};
