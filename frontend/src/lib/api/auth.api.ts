import apiClient from "./client";
import type {
  ApiResponse,
  AuthResponse,
  LoginRequest,
  RegisterRequest,
} from "@/types/auth";

export const authApi = {
  login: (data: LoginRequest) =>
    apiClient.post<ApiResponse<AuthResponse>>("/auth/login", data),

  register: (data: RegisterRequest) =>
    apiClient.post<ApiResponse<AuthResponse>>("/auth/register", data),

  refresh: () => apiClient.post<ApiResponse<AuthResponse>>("/auth/refresh"),

  logout: () => apiClient.post("/auth/logout"),
};
