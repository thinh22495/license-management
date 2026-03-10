"use client";

import { useEffect, useState } from "react";
import { useRouter, usePathname } from "next/navigation";
import { Layout, Menu, Button, Avatar, Dropdown, Typography, Space, Tooltip } from "antd";
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
  RightOutlined,
} from "@ant-design/icons";
import { useAuthStore } from "@/lib/stores/auth.store";
import { formatVND } from "@/lib/utils/format";
import NotificationBell from "@/components/NotificationBell";

const { Header, Sider, Content } = Layout;
const { Text } = Typography;

const mainMenuItems = [
  { key: "/dashboard", icon: <DashboardOutlined />, label: "Dashboard" },
  { key: "/products", icon: <ShoppingCartOutlined />, label: "Mua License" },
  { key: "/licenses", icon: <KeyOutlined />, label: "License của tôi" },
];

const financeMenuItems = [
  { key: "/topup", icon: <WalletOutlined />, label: "Nạp tiền" },
  { key: "/transactions", icon: <HistoryOutlined />, label: "Lịch sử GD" },
];

const otherMenuItems = [
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
        collapsedWidth={72}
        className="custom-sidebar sidebar-user"
        style={{
          position: "fixed",
          left: 0,
          top: 0,
          bottom: 0,
          zIndex: 100,
          background: "linear-gradient(180deg, #1a1640 0%, #252060 50%, #1a1640 100%)",
          boxShadow: "2px 0 24px rgba(0, 0, 0, 0.2)",
          borderRight: "1px solid rgba(255, 255, 255, 0.04)",
          overflow: "hidden",
        }}
      >
        {/* Decorative glow */}
        <div className="sidebar-glow sidebar-glow-user" />

        {/* Logo */}
        <div className="logo-area" style={{ position: "relative", zIndex: 1 }}>
          {collapsed ? (
            <div style={{
              width: 36,
              height: 36,
              borderRadius: 10,
              background: "linear-gradient(135deg, rgba(165, 180, 252, 0.2) 0%, rgba(139, 92, 246, 0.2) 100%)",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              border: "1px solid rgba(165, 180, 252, 0.15)",
            }}>
              <SafetyCertificateOutlined style={{ fontSize: 18, color: "#a5b4fc" }} />
            </div>
          ) : (
            <div style={{ display: "flex", alignItems: "center", gap: 10, width: "100%" }}>
              <div style={{
                width: 36,
                height: 36,
                borderRadius: 10,
                background: "linear-gradient(135deg, rgba(165, 180, 252, 0.2) 0%, rgba(139, 92, 246, 0.2) 100%)",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                border: "1px solid rgba(165, 180, 252, 0.15)",
                flexShrink: 0,
              }}>
                <SafetyCertificateOutlined style={{ fontSize: 18, color: "#a5b4fc" }} />
              </div>
              <div>
                <span className="logo-text">LicenseHub</span>
                <div className="logo-subtitle">Management</div>
              </div>
            </div>
          )}
        </div>

        {/* Menu sections */}
        <div style={{ flex: 1, overflow: "auto", paddingTop: 4 }}>
          {collapsed ? (
            <div className="sidebar-section-label-collapsed"><div className="sidebar-divider-line" /></div>
          ) : (
            <div className="sidebar-section-label">Tổng quan</div>
          )}
          <Menu
            theme="dark"
            mode="inline"
            selectedKeys={[pathname]}
            items={mainMenuItems}
            onClick={({ key }) => router.push(key)}
            style={{ background: "transparent", border: "none" }}
          />

          {collapsed ? (
            <div className="sidebar-section-label-collapsed"><div className="sidebar-divider-line" /></div>
          ) : (
            <div className="sidebar-section-label">Tài chính</div>
          )}
          <Menu
            theme="dark"
            mode="inline"
            selectedKeys={[pathname]}
            items={financeMenuItems}
            onClick={({ key }) => router.push(key)}
            style={{ background: "transparent", border: "none" }}
          />

          {collapsed ? (
            <div className="sidebar-section-label-collapsed"><div className="sidebar-divider-line" /></div>
          ) : (
            <div className="sidebar-section-label">Khác</div>
          )}
          <Menu
            theme="dark"
            mode="inline"
            selectedKeys={[pathname]}
            items={otherMenuItems}
            onClick={({ key }) => router.push(key)}
            style={{ background: "transparent", border: "none" }}
          />
        </div>

        {/* Bottom profile */}
        <Tooltip title={collapsed ? user.fullName : ""} placement="right">
          <div
            className={`sidebar-profile ${collapsed ? "sidebar-profile-collapsed" : ""}`}
            onClick={() => router.push("/profile")}
          >
            <Avatar
              size={collapsed ? 32 : 34}
              style={{
                background: "linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%)",
                fontWeight: 700,
                fontSize: collapsed ? 13 : 14,
                flexShrink: 0,
              }}
            >
              {user.fullName?.charAt(0)?.toUpperCase()}
            </Avatar>
            {!collapsed && (
              <>
                <div style={{ flex: 1, minWidth: 0 }}>
                  <div style={{
                    color: "rgba(255,255,255,0.9)",
                    fontSize: 13,
                    fontWeight: 600,
                    whiteSpace: "nowrap",
                    overflow: "hidden",
                    textOverflow: "ellipsis",
                  }}>{user.fullName}</div>
                  <div style={{
                    color: "rgba(255,255,255,0.35)",
                    fontSize: 11,
                  }}>{formatVND(user.balance)}</div>
                </div>
                <RightOutlined style={{ color: "rgba(255,255,255,0.2)", fontSize: 10 }} />
              </>
            )}
          </div>
        </Tooltip>
      </Sider>
      <Layout style={{ marginLeft: collapsed ? 72 : 240, transition: "margin-left 0.2s ease" }}>
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
