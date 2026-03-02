"use client";

import { Card, Form, Input, Button, Typography, message, Divider, Descriptions } from "antd";
import { useAuthStore } from "@/lib/stores/auth.store";
import { formatVND, formatDate } from "@/lib/utils/format";
import apiClient from "@/lib/api/client";

const { Title } = Typography;

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
    <div style={{ maxWidth: 600 }}>
      <Title level={3}>Hồ sơ cá nhân</Title>

      <Card style={{ marginBottom: 24 }}>
        <Descriptions column={1} bordered size="small">
          <Descriptions.Item label="Email">{user.email}</Descriptions.Item>
          <Descriptions.Item label="Role">{user.role}</Descriptions.Item>
          <Descriptions.Item label="Số dư">{formatVND(user.balance)}</Descriptions.Item>
        </Descriptions>
      </Card>

      <Card title="Chỉnh sửa thông tin">
        <Form
          form={form}
          layout="vertical"
          initialValues={{ fullName: user.fullName }}
          onFinish={handleUpdate}
        >
          <Form.Item name="fullName" label="Họ tên" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Form.Item name="phone" label="Số điện thoại">
            <Input />
          </Form.Item>
          <Button type="primary" htmlType="submit">
            Lưu thay đổi
          </Button>
        </Form>
      </Card>
    </div>
  );
}
