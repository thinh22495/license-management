"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Form, Input, Button, Card, Typography, Space, App } from "antd";
import { MailOutlined, LockOutlined, SafetyCertificateOutlined } from "@ant-design/icons";
import Link from "next/link";
import { authApi } from "@/lib/api/auth.api";
import { useAuthStore } from "@/lib/stores/auth.store";
import type { LoginRequest } from "@/types/auth";

const { Title, Text } = Typography;

export default function LoginPage() {
  const [loading, setLoading] = useState(false);
  const router = useRouter();
  const setAuth = useAuthStore((s) => s.setAuth);
  const { message } = App.useApp();

  const onFinish = async (values: LoginRequest) => {
    setLoading(true);
    try {
      const response = await authApi.login(values);
      const { data } = response.data;

      if (data) {
        setAuth(data.accessToken, data.user);
        message.success("Đăng nhập thành công!");
        router.push(data.user.role === "Admin" ? "/admin/dashboard" : "/dashboard");
      }
    } catch (error: unknown) {
      const err = error as { response?: { data?: { message?: string } } };
      message.error(err.response?.data?.message || "Đăng nhập thất bại");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-bg">
      <div style={{ position: "relative", zIndex: 1, width: "100%", maxWidth: 440, padding: "0 16px" }}>
        <div className="animate-fade-in-up">
          {/* Logo */}
          <div style={{ textAlign: "center", marginBottom: 32 }}>
            <div style={{
              width: 72,
              height: 72,
              borderRadius: 20,
              background: "rgba(255,255,255,0.15)",
              backdropFilter: "blur(10px)",
              display: "inline-flex",
              alignItems: "center",
              justifyContent: "center",
              marginBottom: 16,
              border: "1px solid rgba(255,255,255,0.2)",
            }}>
              <SafetyCertificateOutlined style={{ fontSize: 36, color: "#fff" }} />
            </div>
            <Title level={3} style={{ color: "#fff", margin: 0, fontWeight: 700 }}>
              License Manager
            </Title>
            <Text style={{ color: "rgba(255,255,255,0.7)", fontSize: 14 }}>
              Hệ thống quản lý license chuyên nghiệp
            </Text>
          </div>

          <Card className="auth-card" styles={{ body: { padding: "36px 32px" } }}>
            <div style={{ textAlign: "center", marginBottom: 28 }}>
              <Title level={3} style={{ marginBottom: 4, fontWeight: 700 }}>
                Chào mừng trở lại
              </Title>
              <Text type="secondary">Đăng nhập để tiếp tục</Text>
            </div>

            <Form layout="vertical" onFinish={onFinish} autoComplete="off" size="large">
              <Form.Item
                name="email"
                rules={[
                  { required: true, message: "Vui lòng nhập email" },
                  { type: "email", message: "Email không hợp lệ" },
                ]}
              >
                <Input
                  prefix={<MailOutlined style={{ color: "#a5b4fc" }} />}
                  placeholder="Email"
                  style={{ borderRadius: 12, height: 48 }}
                />
              </Form.Item>

              <Form.Item
                name="password"
                rules={[{ required: true, message: "Vui lòng nhập mật khẩu" }]}
              >
                <Input.Password
                  prefix={<LockOutlined style={{ color: "#a5b4fc" }} />}
                  placeholder="Mật khẩu"
                  style={{ borderRadius: 12, height: 48 }}
                />
              </Form.Item>

              <Form.Item style={{ marginBottom: 16 }}>
                <Button
                  type="primary"
                  htmlType="submit"
                  block
                  loading={loading}
                  className="btn-gradient"
                  style={{ height: 48, borderRadius: 12, fontSize: 16, fontWeight: 600 }}
                >
                  Đăng nhập
                </Button>
              </Form.Item>
            </Form>

            <div style={{ textAlign: "center" }}>
              <Text type="secondary">
                Chưa có tài khoản?{" "}
                <Link href="/register" style={{ fontWeight: 600 }}>Đăng ký ngay</Link>
              </Text>
            </div>
          </Card>
        </div>
      </div>
    </div>
  );
}
