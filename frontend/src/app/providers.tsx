"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { App, ConfigProvider } from "antd";
import viVN from "antd/locale/vi_VN";
import { useState } from "react";

export function Providers({ children }: { children: React.ReactNode }) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            staleTime: 60 * 1000,
            retry: 1,
          },
        },
      })
  );

  return (
    <QueryClientProvider client={queryClient}>
      <ConfigProvider
        locale={viVN}
        theme={{
          token: {
            colorPrimary: "#4f46e5",
            colorInfo: "#4f46e5",
            colorSuccess: "#10b981",
            colorWarning: "#f59e0b",
            colorError: "#ef4444",
            borderRadius: 10,
            colorBgContainer: "#ffffff",
            fontFamily: "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif",
            fontSize: 14,
            colorLink: "#4f46e5",
            colorLinkHover: "#7c3aed",
            controlHeight: 38,
          },
          components: {
            Button: {
              borderRadius: 10,
              controlHeight: 40,
              fontWeight: 500,
              primaryShadow: "0 4px 12px rgba(79, 70, 229, 0.25)",
            },
            Card: {
              borderRadiusLG: 16,
              boxShadowTertiary: "0 1px 3px rgba(0,0,0,0.06), 0 1px 2px rgba(0,0,0,0.04)",
            },
            Input: {
              borderRadius: 10,
              controlHeight: 42,
            },
            Select: {
              borderRadius: 10,
              controlHeight: 42,
            },
            Table: {
              borderRadius: 12,
              headerBg: "#fafafe",
              headerColor: "#374151",
            },
            Menu: {
              itemBorderRadius: 10,
              itemMarginInline: 12,
            },
            Modal: {
              borderRadiusLG: 16,
            },
            Tag: {
              borderRadiusSM: 6,
            },
            Tabs: {
              inkBarColor: "#4f46e5",
              itemActiveColor: "#4f46e5",
              itemSelectedColor: "#4f46e5",
            },
          },
        }}
      >
        <App>{children}</App>
      </ConfigProvider>
    </QueryClientProvider>
  );
}
