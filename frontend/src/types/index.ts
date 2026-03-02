export * from "./auth";

export interface Product {
  id: string;
  name: string;
  slug: string;
  description: string;
  iconUrl?: string;
  websiteUrl?: string;
  isActive: boolean;
  metadata?: string;
  createdAt: string;
  updatedAt: string;
}

export interface LicensePlan {
  id: string;
  productId: string;
  productName: string;
  name: string;
  durationDays: number;
  maxActivations: number;
  price: number;
  features: string;
  isActive: boolean;
  createdAt: string;
}

export interface License {
  id: string;
  userId: string;
  userEmail: string;
  productName: string;
  planName: string;
  licenseKey: string;
  status: string;
  activatedAt?: string;
  expiresAt?: string;
  currentActivations: number;
  maxActivations: number;
  createdAt: string;
}

export interface UserDto {
  id: string;
  email: string;
  phone: string;
  fullName: string;
  role: string;
  balance: number;
  isLocked: boolean;
  emailVerified: boolean;
  avatarUrl?: string;
  createdAt: string;
  updatedAt: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ApiResponse<T = unknown> {
  success: boolean;
  message?: string;
  data?: T;
}
