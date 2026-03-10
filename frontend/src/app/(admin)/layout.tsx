"use client";

import { useEffect, useState } from "react";
import { useRouter, usePathname } from "next/navigation";
import { Layout, Menu, Button, Avatar, Dropdown, Typography, Space, Tooltip } from "antd";
import {
  DashboardOutlined,
  AppstoreOutlined,
  KeyOutlined,
  UserOutlined,
  LogoutOutlined,
  MenuFoldOutlined,
  MenuUnfoldOutlined,
  BellOutlined,
  SafetyCertificateOutlined,
  HomeOutlined,
  RightOutlined,
} from "@ant-design/icons";
import { useAuthStore } from "@/lib/stores/auth.store";

const { Header, Sider, Content } = Layout;
const { Text } = Typography;

const mainMenuItems = [
  { key: "/admin/dashboard", icon: <DashboardOutlined />, label: "Dashboard" },
  { key: "/admin/products", icon: <AppstoreOutlined />, label: "Sản phẩm" },
  { key: "/admin/licenses", icon: <KeyOutlined />, label: "Licenses" },
];

const systemMenuItems = [
  { key: "/admin/users", icon: <UserOutlined />, label: "Người dùng" },
  { key: "/admin/notifications", icon: <BellOutlined />, label: "Thông báo" },
];

export default function AdminLayout({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const { user, isAuthenticated, hasHydrated, logout } = useAuthStore();
  const [collapsed, setCollapsed] = useState(false);

  useEffect(() => {
    if (hasHydrated && (!isAuthenticated || user?.role !== "Admin")) {
      router.push("/login");
    }
  }, [hasHydrated, isAuthenticated, user, router]);

  if (!hasHydrated) return null;
  if (!user || user.role !== "Admin") return null;

  const handleLogout = () => {
    logout();
    router.push("/login");
  };

  const userMenu = {
    items: [
      { key: "user-panel", icon: <HomeOutlined />, label: "User Panel", onClick: () => router.push("/dashboard") },
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
        className="custom-sidebar sidebar-admin"
        style={{
          position: "fixed",
          left: 0,
          top: 0,
          bottom: 0,
          zIndex: 100,
          background: "linear-gradient(180deg, #0c1222 0%, #162036 50%, #0c1222 100%)",
          boxShadow: "2px 0 24px rgba(0, 0, 0, 0.2)",
          borderRight: "1px solid rgba(255, 255, 255, 0.04)",
          overflow: "hidden",
        }}
      >
        {/* Decorative glow */}
        <div className="sidebar-glow sidebar-glow-admin" />

        {/* Logo */}
        <div className="logo-area" style={{ position: "relative", zIndex: 1 }}>
          {collapsed ? (
            <div style={{
              width: 36,
              height: 36,
              borderRadius: 10,
              background: "linear-gradient(135deg, rgba(96, 165, 250, 0.2) 0%, rgba(59, 130, 246, 0.2) 100%)",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              border: "1px solid rgba(96, 165, 250, 0.15)",
            }}>
              <SafetyCertificateOutlined style={{ fontSize: 18, color: "#60a5fa" }} />
            </div>
          ) : (
            <div style={{ display: "flex", alignItems: "center", gap: 10, width: "100%" }}>
              <div style={{
                width: 36,
                height: 36,
                borderRadius: 10,
                background: "linear-gradient(135deg, rgba(96, 165, 250, 0.2) 0%, rgba(59, 130, 246, 0.2) 100%)",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                border: "1px solid rgba(96, 165, 250, 0.15)",
                flexShrink: 0,
              }}>
                <SafetyCertificateOutlined style={{ fontSize: 18, color: "#60a5fa" }} />
              </div>
              <div>
                <span className="logo-text">Admin Panel</span>
                <div className="logo-subtitle">LicenseHub</div>
              </div>
            </div>
          )}
        </div>

        {/* Menu sections */}
        <div style={{ flex: 1, overflow: "auto", paddingTop: 4 }}>
          {collapsed ? (
            <div className="sidebar-section-label-collapsed"><div className="sidebar-divider-line" /></div>
          ) : (
            <div className="sidebar-section-label">Quản lý</div>
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
            <div className="sidebar-section-label">Hệ thống</div>
          )}
          <Menu
            theme="dark"
            mode="inline"
            selectedKeys={[pathname]}
            items={systemMenuItems}
            onClick={({ key }) => router.push(key)}
            style={{ background: "transparent", border: "none" }}
          />
        </div>

        {/* Bottom profile */}
        <Tooltip title={collapsed ? user.fullName : ""} placement="right">
          <div
            className={`sidebar-profile ${collapsed ? "sidebar-profile-collapsed" : ""}`}
            onClick={() => router.push("/admin/dashboard")}
          >
            <Avatar
              size={collapsed ? 32 : 34}
              style={{
                background: "linear-gradient(135deg, #1e40af 0%, #3b82f6 100%)",
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
                  }}>Quản trị viên</div>
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
          <Space size="middle">
            <Dropdown menu={userMenu} placement="bottomRight">
              <div style={{
                cursor: "pointer",
                display: "flex",
                alignItems: "center",
                gap: 10,
                padding: "4px 8px",
                borderRadius: 10,
              }}>
                <Avatar
                  style={{
                    background: "linear-gradient(135deg, #1e40af 0%, #3b82f6 100%)",
                    fontWeight: 600,
                  }}
                >
                  {user.fullName?.charAt(0)?.toUpperCase()}
                </Avatar>
                <div style={{ lineHeight: 1.3 }}>
                  <Text strong style={{ fontSize: 13, display: "block" }}>{user.fullName}</Text>
                  <Text type="secondary" style={{ fontSize: 11 }}>Quản trị viên</Text>
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
