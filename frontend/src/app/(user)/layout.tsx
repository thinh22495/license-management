"use client";

import { useEffect, useState } from "react";
import { useRouter, usePathname } from "next/navigation";
import { Layout, Menu, Button, Avatar, Dropdown, Typography, Space } from "antd";
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
  SafetyCertificateOutlined,
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
      { key: "logout", icon: <LogoutOutlined />, label: "Đăng xuất", onClick: handleLogout, danger: true },
    ],
  };

  return (
    <Layout style={{ minHeight: "100vh" }}>
      <Sider
        trigger={null}
        collapsible
        collapsed={collapsed}
        width={240}
        className="custom-sidebar"
        style={{
          position: "fixed",
          left: 0,
          top: 0,
          bottom: 0,
          zIndex: 100,
          background: "linear-gradient(180deg, #1e1b4b 0%, #312e81 50%, #1e1b4b 100%)",
          boxShadow: "4px 0 20px rgba(0, 0, 0, 0.15)",
        }}
      >
        <div className="logo-area">
          {collapsed ? (
            <div style={{
              width: 40,
              height: 40,
              borderRadius: 12,
              background: "rgba(255,255,255,0.1)",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
            }}>
              <SafetyCertificateOutlined style={{ fontSize: 20, color: "#a5b4fc" }} />
            </div>
          ) : (
            <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
              <div style={{
                width: 40,
                height: 40,
                borderRadius: 12,
                background: "rgba(255,255,255,0.1)",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
              }}>
                <SafetyCertificateOutlined style={{ fontSize: 20, color: "#a5b4fc" }} />
              </div>
              <span className="logo-text">LicenseHub</span>
            </div>
          )}
        </div>
        <Menu
          theme="dark"
          mode="inline"
          selectedKeys={[pathname]}
          items={menuItems}
          onClick={({ key }) => router.push(key)}
          style={{ background: "transparent", border: "none" }}
        />
      </Sider>
      <Layout style={{ marginLeft: collapsed ? 80 : 240, transition: "margin-left 0.2s ease" }}>
        <Header className="app-header" style={{
          padding: "0 28px",
          height: 64,
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          position: "sticky",
          top: 0,
          zIndex: 99,
        }}>
          <Button
            type="text"
            icon={collapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />}
            onClick={() => setCollapsed(!collapsed)}
            style={{ fontSize: 16, width: 40, height: 40 }}
          />
          <Space size="middle" align="center">
            <div style={{
              background: "linear-gradient(135deg, #eef2ff 0%, #e0e7ff 100%)",
              borderRadius: 10,
              padding: "6px 14px",
              display: "flex",
              alignItems: "center",
              gap: 6,
            }}>
              <WalletOutlined style={{ color: "#4f46e5" }} />
              <Text strong className="balance-display">{formatVND(user.balance)}</Text>
            </div>
            <NotificationBell />
            <Dropdown menu={userMenu} placement="bottomRight">
              <div style={{
                cursor: "pointer",
                display: "flex",
                alignItems: "center",
                gap: 10,
                padding: "4px 8px",
                borderRadius: 10,
                transition: "background 0.2s",
              }}>
                <Avatar
                  style={{
                    background: "linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%)",
                    fontWeight: 600,
                  }}
                >
                  {user.fullName?.charAt(0)?.toUpperCase()}
                </Avatar>
                <div style={{ lineHeight: 1.3 }}>
                  <Text strong style={{ fontSize: 13, display: "block" }}>{user.fullName}</Text>
                  <Text type="secondary" style={{ fontSize: 11 }}>{user.role}</Text>
                </div>
              </div>
            </Dropdown>
          </Space>
        </Header>
        <Content className="content-area" style={{ background: "#f0f2f5" }}>
          {children}
        </Content>
      </Layout>
    </Layout>
  );
}
