import apiClient from "./client";
import type { ApiResponse, LicensePlan } from "@/types";

export const plansApi = {
  getByProduct: (productId: string, params?: { activeOnly?: boolean }) =>
    apiClient.get<ApiResponse<LicensePlan[]>>(`/products/${productId}/plans`, { params }),

  create: (productId: string, data: { name: string; durationDays: number; maxActivations: number; price: number; features: string; isActive?: boolean }) =>
    apiClient.post<ApiResponse<LicensePlan>>(`/products/${productId}/plans`, data),

  update: (id: string, data: { name?: string; durationDays?: number; maxActivations?: number; price?: number; features?: string; isActive?: boolean }) =>
    apiClient.put<ApiResponse<LicensePlan>>(`/products/plans/${id}`, data),

  delete: (id: string) =>
    apiClient.delete<ApiResponse>(`/products/plans/${id}`),
};
