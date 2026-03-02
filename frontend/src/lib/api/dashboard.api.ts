import apiClient from "./client";
import type { ApiResponse } from "@/types";

export interface DashboardStats {
  totalUsers: number;
  activeLicenses: number;
  totalProducts: number;
  revenueThisMonth: number;
  newUsersThisMonth: number;
  licensesPurchasedThisMonth: number;
}

export interface RevenueChartItem { date: string; amount: number; }
export interface LicenseChartItem { date: string; count: number; }
export interface UserGrowthItem { date: string; count: number; }
export interface ProductRevenueItem { productName: string; revenue: number; licenseCount: number; }

export interface ChartsData {
  revenue: RevenueChartItem[];
  licenses: LicenseChartItem[];
  users: UserGrowthItem[];
  productRevenue: ProductRevenueItem[];
}

export const dashboardApi = {
  getStats: () =>
    apiClient.get<ApiResponse<DashboardStats>>("/dashboard/stats"),

  getCharts: (days = 30) =>
    apiClient.get<ApiResponse<ChartsData>>("/dashboard/charts", { params: { days } }),
};
