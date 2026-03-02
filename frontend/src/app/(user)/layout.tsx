"use client";

import { useEffect } from "react";
import { useRouter, usePathname } from "next/navigation";
import { Layout, Menu, Avatar, Dropdown, Typography, Space } from "antd";
import {
  DashboardOutlined,
  KeyOutlined,
  ShoppingCartOutlined,
  WalletOutlined,
  HistoryOutlined,
  UserOutlined,
  LogoutOutlined,
  SettingOutlined,
} from "@ant-design/icons";
import { useAuthStore } from "@/lib/stores/auth.store";
import { formatVND } from "@/lib/utils/format";
import NotificationBell from "@/components/NotificationBell";

const { Header, Content } = Layout;
const { Text } = Typography;

const menuItems = [
  { key: "/dashboard", icon: <DashboardOutlined />, label: "Dashboard" },
  { key: "/products", icon: <ShoppingCartOutlined />, label: "Mua License" },
  { key: "/licenses", icon: <KeyOutlined />, label: "License của tôi" },
  { key: "/topup", icon: <WalletOutlined />, label: "Nạp tiền" },
  { key: "/transactions", icon: <HistoryOutlined />, label: "Lịch sử GD" },
];

export default function UserLayout({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const { user, isAuthenticated, hasHydrated, logout } = useAuthStore();

  useEffect(() => {
    if (hasHydrated && !isAuthenticated) {
      router.push("/login");
    }
  }, [hasHydrated, isAuthenticated, router]);

  if (!hasHydrated) return null;
  if (!user) return null;

  const handleLogout = () => {
    logout();
    router.push("/login");
  };

  const userMenu = {
    items: [
      { key: "profile", icon: <SettingOutlined />, label: "Hồ sơ", onClick: () => router.push("/profile") },
      ...(user.role === "Admin"
        ? [{ key: "admin", icon: <DashboardOutlined />, label: "Admin Panel", onClick: () => router.push("/admin/dashboard") }]
        : []),
      { type: "divider" as const },
      { key: "logout", icon: <LogoutOutlined />, label: "Đăng xuất", onClick: handleLogout },
    ],
  };

  return (
    <Layout style={{ minHeight: "100vh" }}>
      <Header style={{ display: "flex", alignItems: "center", justifyContent: "space-between", padding: "0 24px" }}>
        <div style={{ display: "flex", alignItems: "center", gap: 32 }}>
          <Text strong style={{ color: "#fff", fontSize: 18 }}>License Manager</Text>
          <Menu
            theme="dark"
            mode="horizontal"
            selectedKeys={[pathname]}
            items={menuItems}
            onClick={({ key }) => router.push(key)}
            style={{ flex: 1, minWidth: 0 }}
          />
        </div>
        <Space size="middle">
          <NotificationBell />
          <Dropdown menu={userMenu} placement="bottomRight">
            <Space style={{ cursor: "pointer", color: "#fff" }}>
              <Text style={{ color: "#fff" }}>{formatVND(user.balance)}</Text>
              <Avatar icon={<UserOutlined />} />
              <Text style={{ color: "#fff" }}>{user.fullName}</Text>
            </Space>
          </Dropdown>
        </Space>
      </Header>
      <Content style={{ padding: 24, maxWidth: 1200, margin: "0 auto", width: "100%" }}>
        {children}
      </Content>
    </Layout>
  );
}
