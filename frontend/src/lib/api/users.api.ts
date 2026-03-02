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

  create: (data: { email: string; password: string; fullName: string; phone?: string; role?: string }) =>
    apiClient.post<ApiResponse<UserDto>>("/users", data),

  adminUpdate: (id: string, data: { email?: string; fullName?: string; phone?: string; role?: string; emailVerified?: boolean }) =>
    apiClient.put<ApiResponse<UserDto>>(`/users/${id}/admin-update`, data),

  topUp: (id: string, data: { amount: number; note?: string }) =>
    apiClient.post<ApiResponse<UserDto>>(`/users/${id}/topup`, data),
};
