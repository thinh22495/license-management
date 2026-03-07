"use client";

import { Card, Form, Input, Button, Typography, message, Descriptions, Avatar, Space } from "antd";
import { UserOutlined, MailOutlined, SafetyCertificateOutlined, WalletOutlined, SaveOutlined } from "@ant-design/icons";
import { useAuthStore } from "@/lib/stores/auth.store";
import { formatVND } from "@/lib/utils/format";
import apiClient from "@/lib/api/client";

const { Title, Text } = Typography;

export default function ProfilePage() {
  const { user } = useAuthStore();
  const [form] = Form.useForm();

  if (!user) return null;

  const handleUpdate = async (values: { fullName: string; phone: string }) => {
    try {
      await apiClient.put("/me", values);
      message.success("Cập nhật thành công");
    } catch {
      message.error("Lỗi khi cập nhật");
    }
  };

  return (
    <div style={{ maxWidth: 640 }}>
      <Title level={3} className="page-title">
        <UserOutlined style={{ marginRight: 10 }} />
        Hồ sơ cá nhân
      </Title>

      <Card className="enhanced-card" styles={{ body: { padding: 0, overflow: "hidden" } }} style={{ marginBottom: 24 }}>
        {/* Profile header */}
        <div className="profile-header">
          <Avatar
            size={80}
            style={{
              background: "rgba(255,255,255,0.2)",
              fontSize: 32,
              fontWeight: 700,
              marginBottom: 12,
              border: "3px solid rgba(255,255,255,0.3)",
            }}
          >
            {user.fullName?.charAt(0)?.toUpperCase()}
          </Avatar>
          <Title level={4} style={{ color: "#fff", margin: "0 0 4px" }}>{user.fullName}</Title>
          <Text style={{ color: "rgba(255,255,255,0.8)" }}>{user.email}</Text>
        </div>

        <div style={{ padding: "20px 24px" }}>
          <Space direction="vertical" style={{ width: "100%" }} size={12}>
            <div style={{
              display: "flex",
              justifyContent: "space-between",
              alignItems: "center",
              padding: "10px 16px",
              background: "#fafafe",
              borderRadius: 10,
            }}>
              <Space>
                <MailOutlined style={{ color: "#4f46e5" }} />
                <Text type="secondary">Email</Text>
              </Space>
              <Text strong>{user.email}</Text>
            </div>
            <div style={{
              display: "flex",
              justifyContent: "space-between",
              alignItems: "center",
              padding: "10px 16px",
              background: "#fafafe",
              borderRadius: 10,
            }}>
              <Space>
                <SafetyCertificateOutlined style={{ color: "#4f46e5" }} />
                <Text type="secondary">Vai trò</Text>
              </Space>
              <Text strong>{user.role}</Text>
            </div>
            <div style={{
              display: "flex",
              justifyContent: "space-between",
              alignItems: "center",
              padding: "10px 16px",
              background: "#f0fdf4",
              borderRadius: 10,
              border: "1px solid #bbf7d0",
            }}>
              <Space>
                <WalletOutlined style={{ color: "#10b981" }} />
                <Text type="secondary">Số dư</Text>
              </Space>
              <Text strong style={{ color: "#10b981", fontSize: 16 }}>{formatVND(user.balance)}</Text>
            </div>
          </Space>
        </div>
      </Card>

      <Card className="enhanced-card" title={<Text strong style={{ fontSize: 16 }}>Chỉnh sửa thông tin</Text>}>
        <Form
          form={form}
          layout="vertical"
          initialValues={{ fullName: user.fullName }}
          onFinish={handleUpdate}
        >
          <Form.Item name="fullName" label="Họ tên" rules={[{ required: true }]}>
            <Input style={{ borderRadius: 10 }} size="large" />
          </Form.Item>
          <Form.Item name="phone" label="Số điện thoại">
            <Input style={{ borderRadius: 10 }} size="large" />
          </Form.Item>
          <Button
            type="primary"
            htmlType="submit"
            icon={<SaveOutlined />}
            className="btn-gradient"
            style={{ borderRadius: 10, height: 44, fontWeight: 600 }}
          >
            Lưu thay đổi
          </Button>
        </Form>
      </Card>
    </div>
  );
}
