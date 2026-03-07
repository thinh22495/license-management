"use client";

import { useState } from "react";
import {
  Card,
  Form,
  Input,
  Select,
  Button,
  Typography,
  App,
  Switch,
  Space,
  Checkbox,
} from "antd";
import { SendOutlined, BellOutlined } from "@ant-design/icons";
import { useMutation, useQuery } from "@tanstack/react-query";
import { notificationsApi } from "@/lib/api/notifications.api";
import { usersApi } from "@/lib/api/users.api";

const { Title, Text } = Typography;
const { TextArea } = Input;

export default function AdminNotificationsPage() {
  const [form] = Form.useForm();
  const [isBroadcast, setIsBroadcast] = useState(true);
  const { message } = App.useApp();

  const { data: usersRes } = useQuery({
    queryKey: ["admin-users-select"],
    queryFn: () => usersApi.getAll({ pageSize: 100 }),
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    select: (res) => (res.data as any)?.items ?? [],
  });

  const sendNotification = useMutation({
    mutationFn: (values: { userId?: string; title: string; body: string; type?: string; channels: string[] }) =>
      notificationsApi.send(values),
    onSuccess: () => {
      message.success("Đã gửi thông báo");
      form.resetFields();
    },
    onError: () => message.error("Gửi thông báo thất bại"),
  });

  const handleSubmit = (values: Record<string, any>) => {
    sendNotification.mutate({
      userId: isBroadcast ? undefined : values.userId,
      title: values.title,
      body: values.body,
      type: values.type,
      channels: values.channels || ["web"],
    });
  };

  return (
    <div>
      <Title level={3} className="page-title">
        <BellOutlined style={{ marginRight: 10 }} />
        Gửi thông báo
      </Title>

      <Card className="enhanced-card" style={{ maxWidth: 700 }}>
        <Form form={form} layout="vertical" onFinish={handleSubmit} initialValues={{ type: "Info", channels: ["web"] }}>
          <Form.Item label="Đối tượng">
            <Space align="center">
              <Switch
                checked={isBroadcast}
                onChange={setIsBroadcast}
                checkedChildren="Broadcast"
                unCheckedChildren="Cá nhân"
              />
              <Text type="secondary">
                {isBroadcast ? "Gửi đến tất cả người dùng" : "Gửi đến một người dùng cụ thể"}
              </Text>
            </Space>
          </Form.Item>

          {!isBroadcast && (
            <Form.Item name="userId" label="Người nhận" rules={[{ required: !isBroadcast }]}>
              <Select
                showSearch
                placeholder="Chọn người dùng"
                optionFilterProp="label"
                options={(usersRes ?? []).map((u: any) => ({
                  value: u.id,
                  label: `${u.fullName} (${u.email})`,
                }))}
              />
            </Form.Item>
          )}

          <Form.Item name="title" label="Tiêu đề" rules={[{ required: true }]}>
            <Input placeholder="Tiêu đề thông báo" style={{ borderRadius: 10 }} />
          </Form.Item>

          <Form.Item name="body" label="Nội dung" rules={[{ required: true }]}>
            <TextArea rows={4} placeholder="Nội dung thông báo" style={{ borderRadius: 10 }} />
          </Form.Item>

          <Form.Item name="type" label="Loại">
            <Select
              options={[
                { value: "Info", label: "Thông tin" },
                { value: "Warning", label: "Cảnh báo" },
                { value: "Success", label: "Thành công" },
                { value: "Error", label: "Lỗi" },
              ]}
            />
          </Form.Item>

          <Form.Item name="channels" label="Kênh gửi">
            <Checkbox.Group
              options={[
                { label: "Web (in-app)", value: "web" },
                { label: "Email", value: "email" },
              ]}
            />
          </Form.Item>

          <Button
            type="primary"
            htmlType="submit"
            icon={<SendOutlined />}
            loading={sendNotification.isPending}
            size="large"
            className="btn-gradient"
            style={{ borderRadius: 10, height: 48, fontWeight: 600 }}
          >
            Gửi thông báo
          </Button>
        </Form>
      </Card>
    </div>
  );
}
