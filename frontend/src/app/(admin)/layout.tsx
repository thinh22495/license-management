"use client";

import { useEffect, useState } from "react";
import { useRouter, usePathname } from "next/navigation";
import { Layout, Menu, Button, Avatar, Dropdown, Typography, Space } from "antd";
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
} from "@ant-design/icons";
import { useAuthStore } from "@/lib/stores/auth.store";

const { Header, Sider, Content } = Layout;
const { Text } = Typography;

const menuItems = [
  { key: "/admin/dashboard", icon: <DashboardOutlined />, label: "Dashboard" },
  { key: "/admin/products", icon: <AppstoreOutlined />, label: "Sản phẩm" },
  { key: "/admin/licenses", icon: <KeyOutlined />, label: "Licenses" },
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
        className="custom-sidebar"
        style={{
          position: "fixed",
          left: 0,
          top: 0,
          bottom: 0,
          zIndex: 100,
          background: "linear-gradient(180deg, #0f172a 0%, #1e293b 50%, #0f172a 100%)",
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
              <SafetyCertificateOutlined style={{ fontSize: 20, color: "#60a5fa" }} />
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
                <SafetyCertificateOutlined style={{ fontSize: 20, color: "#60a5fa" }} />
              </div>
              <div>
                <span className="logo-text" style={{ fontSize: 18 }}>Admin Panel</span>
              </div>
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
