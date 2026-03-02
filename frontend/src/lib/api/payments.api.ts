import apiClient from "./client";
import type { ApiResponse, PagedResult } from "@/types";

export interface TopUpRequest {
  amount: number;
  paymentMethod: string;
  returnUrl: string;
}

export interface TopUpResult {
  transactionId: string;
  paymentUrl?: string;
  status: string;
}

export interface TransactionDto {
  id: string;
  type: string;
  amount: number;
  balanceBefore: number;
  balanceAfter: number;
  paymentMethod?: string;
  paymentRef?: string;
  status: string;
  relatedLicenseId?: string;
  createdAt: string;
}

export const paymentsApi = {
  topUp: (data: TopUpRequest) =>
    apiClient.post<ApiResponse<TopUpResult>>("/payments/topup", data),

  getTransactions: (params?: { page?: number; pageSize?: number }) =>
    apiClient.get<PagedResult<TransactionDto>>("/payments/transactions", { params }),
};
