import apiClient from "./client";
import type { ApiResponse, License, PagedResult } from "@/types";

export const licensesApi = {
  // User endpoints
  purchase: (licensePlanId: string) =>
    apiClient.post<ApiResponse<License>>("/licenses/purchase", { licensePlanId }),

  getMyLicenses: () =>
    apiClient.get<ApiResponse<License[]>>("/licenses/my"),

  renew: (licenseId: string) =>
    apiClient.post<ApiResponse<License>>("/licenses/renew", { licenseId }),

  // Admin endpoints
  getAll: (params?: { page?: number; pageSize?: number; status?: string; productId?: string; search?: string }) =>
    apiClient.get<PagedResult<License>>("/licenses", { params }),

  revoke: (id: string) =>
    apiClient.put<ApiResponse>(`/licenses/${id}/revoke`),

  suspend: (id: string) =>
    apiClient.put<ApiResponse>(`/licenses/${id}/suspend`),

  reinstate: (id: string) =>
    apiClient.put<ApiResponse>(`/licenses/${id}/reinstate`),
};
