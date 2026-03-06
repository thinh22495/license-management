"use client";

import { useEffect, useState } from "react";
import { useRouter, usePathname } from "next/navigation";
import { Layout, Menu, Button, Avatar, Dropdown, Typography, Space, Badge } from "antd";
import {
  DashboardOutlined,
  KeyOutlined,
  ShoppingCartOutlined,
  WalletOutlined,
  HistoryOutlined,
  UserOutlined,
  LogoutOutlined,
  SettingOutlined,
  MenuFoldOutlined,
  MenuUnfoldOutlined,
  BellOutlined,
} from "@ant-design/icons";
import { useAuthStore } from "@/lib/stores/auth.store";
import { formatVND } from "@/lib/utils/format";
import NotificationBell from "@/components/NotificationBell";

const { Header, Sider, Content } = Layout;
const { Text } = Typography;

const menuItems = [
  { key: "/dashboard", icon: <DashboardOutlined />, label: "Dashboard" },
  { key: "/products", icon: <ShoppingCartOutlined />, label: "Mua License" },
  { key: "/licenses", icon: <KeyOutlined />, label: "License của tôi" },
  { key: "/topup", icon: <WalletOutlined />, label: "Nạp tiền" },
  { key: "/transactions", icon: <HistoryOutlined />, label: "Lịch sử GD" },
  { key: "/notifications", icon: <BellOutlined />, label: "Thông báo" },
];

export default function UserLayout({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const { user, isAuthenticated, hasHydrated, logout } = useAuthStore();
  const [collapsed, setCollapsed] = useState(false);

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
      <Sider
        trigger={null}
        collapsible
        collapsed={collapsed}
        theme="dark"
        style={{ position: "fixed", left: 0, top: 0, bottom: 0, zIndex: 100 }}
      >
        <div style={{ height: 64, display: "flex", alignItems: "center", justifyContent: "center", color: "#fff" }}>
          <Text strong style={{ color: "#fff", fontSize: collapsed ? 14 : 16 }}>
            {collapsed ? "LM" : "License Manager"}
          </Text>
        </div>
        <Menu
          theme="dark"
          mode="inline"
          selectedKeys={[pathname]}
          items={menuItems}
          onClick={({ key }) => router.push(key)}
        />
      </Sider>
      <Layout style={{ marginLeft: collapsed ? 80 : 200, transition: "margin-left 0.2s" }}>
        <Header style={{
          padding: "0 24px",
          background: "#fff",
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          borderBottom: "1px solid #f0f0f0",
        }}>
          <Button
            type="text"
            icon={collapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />}
            onClick={() => setCollapsed(!collapsed)}
          />
          <Space size="middle">
            <Text strong style={{ color: "#1677ff" }}>{formatVND(user.balance)}</Text>
            <NotificationBell />
            <Dropdown menu={userMenu} placement="bottomRight">
              <div style={{ cursor: "pointer", display: "flex", alignItems: "center", gap: 8 }}>
                <Avatar icon={<UserOutlined />} />
                <Text>{user.fullName}</Text>
              </div>
            </Dropdown>
          </Space>
        </Header>
        <Content style={{ margin: 24, minHeight: 280 }}>
          {children}
        </Content>
      </Layout>
    </Layout>
  );
}
