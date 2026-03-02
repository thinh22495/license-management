"use client";

import { useEffect, useState } from "react";
import { useRouter, usePathname } from "next/navigation";
import { Layout, Menu, Button, Avatar, Dropdown, Typography } from "antd";
import {
  DashboardOutlined,
  AppstoreOutlined,
  KeyOutlined,
  UserOutlined,
  LogoutOutlined,
  MenuFoldOutlined,
  MenuUnfoldOutlined,
  BellOutlined,
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
  const { user, isAuthenticated, logout } = useAuthStore();
  const [collapsed, setCollapsed] = useState(false);

  useEffect(() => {
    if (!isAuthenticated || user?.role !== "Admin") {
      router.push("/login");
    }
  }, [isAuthenticated, user, router]);

  if (!user || user.role !== "Admin") return null;

  const handleLogout = () => {
    logout();
    router.push("/login");
  };

  const userMenu = {
    items: [
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
            {collapsed ? "LM" : "License Admin"}
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
        <Header style={{ padding: "0 24px", background: "#fff", display: "flex", alignItems: "center", justifyContent: "space-between", borderBottom: "1px solid #f0f0f0" }}>
          <Button
            type="text"
            icon={collapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />}
            onClick={() => setCollapsed(!collapsed)}
          />
          <Dropdown menu={userMenu} placement="bottomRight">
            <div style={{ cursor: "pointer", display: "flex", alignItems: "center", gap: 8 }}>
              <Avatar icon={<UserOutlined />} />
              <Text>{user.fullName}</Text>
            </div>
          </Dropdown>
        </Header>
        <Content style={{ margin: 24, minHeight: 280 }}>
          {children}
        </Content>
      </Layout>
    </Layout>
  );
}
