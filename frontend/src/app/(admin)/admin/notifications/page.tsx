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
  Table,
  Tag,
  Popconfirm,
  Tabs,
  Modal,
} from "antd";
import {
  SendOutlined,
  BellOutlined,
  DeleteOutlined,
  EyeOutlined,
  SearchOutlined,
} from "@ant-design/icons";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { notificationsApi, type AdminNotificationDto } from "@/lib/api/notifications.api";
import { usersApi } from "@/lib/api/users.api";
import { formatDate } from "@/lib/utils/format";
import type { PagedResult } from "@/types";

const { Title, Text } = Typography;
const { TextArea } = Input;
const { Search } = Input;

const typeColorMap: Record<string, string> = {
  Info: "blue",
  Warning: "orange",
  Success: "green",
  Error: "red",
};

const typeLabelMap: Record<string, string> = {
  Info: "Thông tin",
  Warning: "Cảnh báo",
  Success: "Thành công",
  Error: "Lỗi",
};

export default function AdminNotificationsPage() {
  const [form] = Form.useForm();
  const [isBroadcast, setIsBroadcast] = useState(true);
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  // List state
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [filterType, setFilterType] = useState<string | undefined>(undefined);
  const [detailNotification, setDetailNotification] = useState<AdminNotificationDto | null>(null);

  const { data: usersRes } = useQuery({
    queryKey: ["admin-users-select"],
    queryFn: () => usersApi.getAll({ pageSize: 100 }),
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    select: (res) => (res.data as any)?.items ?? [],
  });

  const { data: notificationsRes, isLoading } = useQuery({
    queryKey: ["admin-notifications", page, search, filterType],
    queryFn: () =>
      notificationsApi.adminGetAll({
        page,
        pageSize: 20,
        search: search || undefined,
        type: filterType,
      }),
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    select: (res) => res.data as any as PagedResult<AdminNotificationDto>,
  });

  const sendNotification = useMutation({
    mutationFn: (values: { userId?: string; title: string; body: string; type?: string; channels: string[] }) =>
      notificationsApi.send(values),
    onSuccess: () => {
      message.success("Đã gửi thông báo");
      form.resetFields();
      queryClient.invalidateQueries({ queryKey: ["admin-notifications"] });
    },
    onError: () => message.error("Gửi thông báo thất bại"),
  });

  const deleteNotification = useMutation({
    mutationFn: (id: string) => notificationsApi.adminDelete(id),
    onSuccess: () => {
      message.success("Đã xóa thông báo");
      queryClient.invalidateQueries({ queryKey: ["admin-notifications"] });
    },
    onError: () => message.error("Xóa thông báo thất bại"),
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

  const columns = [
    {
      title: "Tiêu đề",
      dataIndex: "title",
      key: "title",
      ellipsis: true,
      render: (v: string) => <Text strong>{v}</Text>,
    },
    {
      title: "Người nhận",
      key: "user",
      width: 200,
      render: (_: unknown, r: AdminNotificationDto) =>
        r.userId ? (
          <div>
            <Text>{r.userFullName}</Text>
            <br />
            <Text type="secondary" style={{ fontSize: 12 }}>{r.userEmail}</Text>
          </div>
        ) : (
          <Tag color="purple" style={{ borderRadius: 6 }}>Broadcast</Tag>
        ),
    },
    {
      title: "Loại",
      dataIndex: "type",
      key: "type",
      width: 110,
      render: (v: string) => (
        <Tag color={typeColorMap[v] || "default"} style={{ borderRadius: 6, fontWeight: 500 }}>
          {typeLabelMap[v] || v}
        </Tag>
      ),
    },
    {
      title: "Trạng thái",
      dataIndex: "isRead",
      key: "isRead",
      width: 110,
      render: (v: boolean) =>
        v ? (
          <Tag color="default" style={{ borderRadius: 6 }}>Đã đọc</Tag>
        ) : (
          <Tag color="blue" style={{ borderRadius: 6 }}>Chưa đọc</Tag>
        ),
    },
    {
      title: "Ngày tạo",
      dataIndex: "createdAt",
      key: "createdAt",
      width: 160,
      render: (v: string) => formatDate(v),
    },
    {
      title: "Thao tác",
      key: "actions",
      width: 150,
      render: (_: unknown, record: AdminNotificationDto) => (
        <Space>
          <Button
            size="small"
            icon={<EyeOutlined />}
            onClick={() => setDetailNotification(record)}
            style={{ borderRadius: 8 }}
          >
            Xem
          </Button>
          <Popconfirm
            title="Xác nhận xóa thông báo này?"
            onConfirm={() => deleteNotification.mutate(record.id)}
          >
            <Button
              size="small"
              danger
              icon={<DeleteOutlined />}
              style={{ borderRadius: 8 }}
            >
              Xóa
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  const sendForm = (
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
  );

  const listView = (
    <Card className="enhanced-card" styles={{ body: { padding: "16px 0 0" } }}>
      <div style={{ padding: "0 16px", marginBottom: 16, display: "flex", gap: 12 }}>
        <Search
          placeholder="Tìm theo tiêu đề, nội dung, email..."
          allowClear
          onSearch={(v) => { setSearch(v); setPage(1); }}
          style={{ width: 320 }}
        />
        <Select
          placeholder="Lọc theo loại"
          allowClear
          onChange={(v) => { setFilterType(v); setPage(1); }}
          style={{ width: 160 }}
          options={[
            { value: "Info", label: "Thông tin" },
            { value: "Warning", label: "Cảnh báo" },
            { value: "Success", label: "Thành công" },
            { value: "Error", label: "Lỗi" },
          ]}
        />
      </div>

      <Table
        columns={columns}
        dataSource={notificationsRes?.items ?? []}
        rowKey="id"
        loading={isLoading}
        pagination={{
          current: page,
          total: notificationsRes?.totalCount ?? 0,
          pageSize: 20,
          onChange: setPage,
          showTotal: (total) => `Tổng ${total} thông báo`,
        }}
      />
    </Card>
  );

  return (
    <div>
      <Title level={3} className="page-title" style={{ marginBottom: 20 }}>
        <BellOutlined style={{ marginRight: 10 }} />
        Quản lý thông báo
      </Title>

      <Tabs
        defaultActiveKey="list"
        items={[
          {
            key: "list",
            label: "Danh sách thông báo",
            children: listView,
          },
          {
            key: "send",
            label: "Gửi thông báo",
            children: sendForm,
          },
        ]}
      />

      {/* Detail Modal */}
      <Modal
        title={<Text strong style={{ fontSize: 16 }}>Chi tiết thông báo</Text>}
        open={!!detailNotification}
        onCancel={() => setDetailNotification(null)}
        footer={[
          <Button key="close" onClick={() => setDetailNotification(null)}>
            Đóng
          </Button>,
          <Popconfirm
            key="delete"
            title="Xác nhận xóa thông báo này?"
            onConfirm={() => {
              if (detailNotification) {
                deleteNotification.mutate(detailNotification.id);
                setDetailNotification(null);
              }
            }}
          >
            <Button danger icon={<DeleteOutlined />}>
              Xóa
            </Button>
          </Popconfirm>,
        ]}
        width={600}
      >
        {detailNotification && (
          <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
            <div>
              <Text type="secondary">Tiêu đề</Text>
              <div><Text strong style={{ fontSize: 16 }}>{detailNotification.title}</Text></div>
            </div>
            <div>
              <Text type="secondary">Nội dung</Text>
              <div style={{
                background: "#f5f5f5",
                padding: "12px 16px",
                borderRadius: 10,
                marginTop: 4,
                whiteSpace: "pre-wrap",
              }}>
                {detailNotification.body}
              </div>
            </div>
            <div style={{ display: "flex", gap: 24 }}>
              <div>
                <Text type="secondary">Loại</Text>
                <div>
                  <Tag color={typeColorMap[detailNotification.type] || "default"} style={{ borderRadius: 6 }}>
                    {typeLabelMap[detailNotification.type] || detailNotification.type}
                  </Tag>
                </div>
              </div>
              <div>
                <Text type="secondary">Trạng thái</Text>
                <div>
                  <Tag color={detailNotification.isRead ? "default" : "blue"} style={{ borderRadius: 6 }}>
                    {detailNotification.isRead ? "Đã đọc" : "Chưa đọc"}
                  </Tag>
                </div>
              </div>
              <div>
                <Text type="secondary">Ngày tạo</Text>
                <div><Text>{formatDate(detailNotification.createdAt)}</Text></div>
              </div>
            </div>
            <div>
              <Text type="secondary">Người nhận</Text>
              <div>
                {detailNotification.userId ? (
                  <Text>{detailNotification.userFullName} ({detailNotification.userEmail})</Text>
                ) : (
                  <Tag color="purple" style={{ borderRadius: 6 }}>Broadcast - Tất cả người dùng</Tag>
                )}
              </div>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
}
