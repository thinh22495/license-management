import apiClient from "./client";
import type { ApiResponse, Product } from "@/types";

export const productsApi = {
  getAll: (params?: { search?: string; activeOnly?: boolean }) =>
    apiClient.get<ApiResponse<Product[]>>("/products", { params }),

  getBySlug: (slug: string) =>
    apiClient.get<ApiResponse<Product>>(`/products/${slug}`),

  create: (data: { name: string; slug: string; description: string; websiteUrl?: string; isActive?: boolean }) =>
    apiClient.post<ApiResponse<Product>>("/products", data),

  update: (id: string, data: { name?: string; description?: string; websiteUrl?: string; isActive?: boolean }) =>
    apiClient.put<ApiResponse<Product>>(`/products/${id}`, data),

  delete: (id: string) =>
    apiClient.delete<ApiResponse>(`/products/${id}`),
};
