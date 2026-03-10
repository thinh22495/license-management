"use client";

import { useState } from "react";
import {
  Card,
  Table,
  Tag,
  Space,
  Button,
  Input,
  Typography,
  App,
  Popconfirm,
  Modal,
  Form,
  InputNumber,
  Select,
  Switch,
} from "antd";
import {
  LockOutlined,
  UnlockOutlined,
  PlusOutlined,
  EditOutlined,
  DollarOutlined,
  UserOutlined,
} from "@ant-design/icons";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { usersApi } from "@/lib/api/users.api";
import { formatVND, formatDate } from "@/lib/utils/format";
import type { UserDto, PagedResult } from "@/types";

const { Title, Text } = Typography;
const { Search } = Input;

export default function AdminUsersPage() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");

  const [createOpen, setCreateOpen] = useState(false);
  const [editOpen, setEditOpen] = useState(false);
  const [topUpOpen, setTopUpOpen] = useState(false);
  const [editingUser, setEditingUser] = useState<UserDto | null>(null);
  const [topUpUser, setTopUpUser] = useState<UserDto | null>(null);

  const [createForm] = Form.useForm();
  const [editForm] = Form.useForm();
  const [topUpForm] = Form.useForm();

  const { data: usersRes, isLoading } = useQuery({
    queryKey: ["admin-users", page, search],
    queryFn: () => usersApi.getAll({ page, pageSize: 20, search: search || undefined }),
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    select: (res) => res.data as any as PagedResult<UserDto>,
  });

  const toggleLock = useMutation({
    mutationFn: ({ id, isLocked }: { id: string; isLocked: boolean }) => usersApi.lock(id, isLocked),
    onSuccess: (_, variables) => {
      message.success(variables.isLocked ? "Đã khóa tài khoản" : "Đã mở khóa tài khoản");
      queryClient.invalidateQueries({ queryKey: ["admin-users"] });
    },
    onError: () => message.error("Lỗi khi thay đổi trạng thái"),
  });

  const createUser = useMutation({
    mutationFn: (values: { email: string; password: string; fullName: string; phone?: string; role?: string }) =>
      usersApi.create(values),
    onSuccess: () => {
      message.success("Tạo người dùng thành công");
      queryClient.invalidateQueries({ queryKey: ["admin-users"] });
      setCreateOpen(false);
      createForm.resetFields();
    },
    onError: () => message.error("Lỗi khi tạo người dùng"),
  });

  const updateUser = useMutation({
    mutationFn: ({ id, ...data }: { id: string; email?: string; fullName?: string; phone?: string; role?: string; emailVerified?: boolean }) =>
      usersApi.adminUpdate(id, data),
    onSuccess: () => {
      message.success("Cập nhật thành công");
      queryClient.invalidateQueries({ queryKey: ["admin-users"] });
      setEditOpen(false);
      setEditingUser(null);
    },
    onError: () => message.error("Lỗi khi cập nhật"),
  });

  const topUp = useMutation({
    mutationFn: ({ id, amount, note }: { id: string; amount: number; note?: string }) =>
      usersApi.topUp(id, { amount, note }),
    onSuccess: () => {
      message.success("Nạp tiền thành công");
      queryClient.invalidateQueries({ queryKey: ["admin-users"] });
      setTopUpOpen(false);
      setTopUpUser(null);
      topUpForm.resetFields();
    },
    onError: () => message.error("Lỗi khi nạp tiền"),
  });

  const openEdit = (user: UserDto) => {
    setEditingUser(user);
    editForm.setFieldsValue({
      email: user.email,
      fullName: user.fullName,
      phone: user.phone,
      role: user.role,
      emailVerified: user.emailVerified,
    });
    setEditOpen(true);
  };

  const openTopUp = (user: UserDto) => {
    setTopUpUser(user);
    topUpForm.resetFields();
    setTopUpOpen(true);
  };

  const columns = [
    { title: "Email", dataIndex: "email", key: "email", render: (v: string) => <Text strong>{v}</Text> },
    { title: "Họ tên", dataIndex: "fullName", key: "fullName" },
    { title: "SĐT", dataIndex: "phone", key: "phone" },
    {
      title: "Role",
      dataIndex: "role",
      key: "role",
      render: (v: string) => <Tag color={v === "Admin" ? "purple" : "blue"} style={{ borderRadius: 6, fontWeight: 500 }}>{v}</Tag>,
    },
    {
      title: "Số dư",
      dataIndex: "balance",
      key: "balance",
      render: (v: number) => <Text strong style={{ color: "#10b981" }}>{formatVND(v)}</Text>,
    },
    {
      title: "Trạng thái",
      key: "status",
      render: (_: unknown, r: UserDto) => (
        <Space>
          {r.isLocked && <Tag color="red" style={{ borderRadius: 6 }}>Bị khóa</Tag>}
          {r.emailVerified
            ? <Tag color="green" style={{ borderRadius: 6 }}>Verified</Tag>
            : <Tag style={{ borderRadius: 6 }}>Chưa verify</Tag>}
        </Space>
      ),
    },
    {
      title: "Ngày tạo",
      dataIndex: "createdAt",
      key: "createdAt",
      render: (v: string) => formatDate(v),
    },
    {
      title: "Thao tác",
      key: "actions",
      render: (_: unknown, record: UserDto) => (
        <Space>
          <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(record)} style={{ borderRadius: 8 }}>
            Sửa
          </Button>
          <Button size="small" type="primary" icon={<DollarOutlined />} onClick={() => openTopUp(record)} style={{ borderRadius: 8 }}>
            Nạp tiền
          </Button>
          {record.role !== "Admin" && (
            <Popconfirm
              title={record.isLocked ? "Mở khóa tài khoản này?" : "Khóa tài khoản này?"}
              onConfirm={() => toggleLock.mutate({ id: record.id, isLocked: !record.isLocked })}
            >
              <Button
                size="small"
                danger={!record.isLocked}
                icon={record.isLocked ? <UnlockOutlined /> : <LockOutlined />}
                style={{ borderRadius: 8 }}
              >
                {record.isLocked ? "Mở khóa" : "Khóa"}
              </Button>
            </Popconfirm>
          )}
        </Space>
      ),
    },
  ];

  return (
    <div>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 20 }}>
        <Title level={3} className="page-title" style={{ margin: 0 }}>
          <UserOutlined style={{ marginRight: 10 }} />
          Quản lý người dùng
        </Title>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => setCreateOpen(true)} className="btn-gradient" style={{ borderRadius: 10 }}>
          Thêm người dùng
        </Button>
      </div>

      <Card className="enhanced-card" styles={{ body: { padding: "16px 0 0" } }}>
        <div style={{ padding: "0 16px", marginBottom: 16 }}>
          <Search
            placeholder="Tìm theo email hoặc tên..."
            allowClear
            onSearch={(v) => { setSearch(v); setPage(1); }}
            style={{ width: 320, borderRadius: 10 }}
          />
        </div>

        <Table
          columns={columns}
          dataSource={usersRes?.items ?? []}
          rowKey="id"
          loading={isLoading}
          pagination={{
            current: page,
            total: usersRes?.totalCount ?? 0,
            pageSize: 20,
            onChange: setPage,
            showTotal: (total) => `Tổng ${total} người dùng`,
          }}
        />
      </Card>

      {/* Create User Modal */}
      <Modal
        title={<Text strong style={{ fontSize: 16 }}>Thêm người dùng</Text>}
        open={createOpen}
        onOk={() => createForm.submit()}
        onCancel={() => { setCreateOpen(false); createForm.resetFields(); }}
        confirmLoading={createUser.isPending}
      >
        <Form form={createForm} layout="vertical" onFinish={(v) => createUser.mutate(v)}>
          <Form.Item name="email" label="Email" rules={[{ required: true, type: "email", message: "Vui lòng nhập email hợp lệ" }]}>
            <Input style={{ borderRadius: 10 }} />
          </Form.Item>
          <Form.Item name="password" label="Mật khẩu" rules={[{ required: true, min: 6, message: "Mật khẩu tối thiểu 6 ký tự" }]}>
            <Input.Password style={{ borderRadius: 10 }} />
          </Form.Item>
          <Form.Item name="fullName" label="Họ tên" rules={[{ required: true, message: "Vui lòng nhập họ tên" }]}>
            <Input style={{ borderRadius: 10 }} />
          </Form.Item>
          <Form.Item name="phone" label="Số điện thoại">
            <Input style={{ borderRadius: 10 }} />
          </Form.Item>
          <Form.Item name="role" label="Vai trò" initialValue="User">
            <Select>
              <Select.Option value="User">User</Select.Option>
              <Select.Option value="Admin">Admin</Select.Option>
            </Select>
          </Form.Item>
        </Form>
      </Modal>

      {/* Edit User Modal */}
      <Modal
        title={<Text strong style={{ fontSize: 16 }}>Sửa người dùng: {editingUser?.email ?? ""}</Text>}
        open={editOpen}
        onOk={() => editForm.submit()}
        onCancel={() => { setEditOpen(false); setEditingUser(null); }}
        confirmLoading={updateUser.isPending}
      >
        <Form
          form={editForm}
          layout="vertical"
          onFinish={(v) => editingUser && updateUser.mutate({ id: editingUser.id, ...v })}
        >
          <Form.Item name="email" label="Email" rules={[{ required: true, type: "email", message: "Vui lòng nhập email hợp lệ" }]}>
            <Input style={{ borderRadius: 10 }} />
          </Form.Item>
          <Form.Item name="fullName" label="Họ tên" rules={[{ required: true, message: "Vui lòng nhập họ tên" }]}>
            <Input style={{ borderRadius: 10 }} />
          </Form.Item>
          <Form.Item name="phone" label="Số điện thoại">
            <Input style={{ borderRadius: 10 }} />
          </Form.Item>
          <Form.Item name="role" label="Vai trò">
            <Select>
              <Select.Option value="User">User</Select.Option>
              <Select.Option value="Admin">Admin</Select.Option>
            </Select>
          </Form.Item>
          <Form.Item name="emailVerified" label="Email đã xác thực" valuePropName="checked">
            <Switch />
          </Form.Item>
        </Form>
      </Modal>

      {/* Top Up Modal */}
      <Modal
        title={<Text strong style={{ fontSize: 16 }}>Nạp tiền cho: {topUpUser?.email ?? ""}</Text>}
        open={topUpOpen}
        onOk={() => topUpForm.submit()}
        onCancel={() => { setTopUpOpen(false); setTopUpUser(null); topUpForm.resetFields(); }}
        confirmLoading={topUp.isPending}
      >
        {topUpUser && (
          <div style={{
            background: "linear-gradient(135deg, #f0fdf4, #ecfdf5)",
            borderRadius: 10,
            padding: "12px 16px",
            marginBottom: 16,
            border: "1px solid #bbf7d0",
          }}>
            <Text type="secondary">Số dư hiện tại: </Text>
            <Text strong style={{ color: "#10b981", fontSize: 16 }}>{formatVND(topUpUser.balance)}</Text>
          </div>
        )}
        <Form
          form={topUpForm}
          layout="vertical"
          onFinish={(v) => topUpUser && topUp.mutate({ id: topUpUser.id, ...v })}
        >
          <Form.Item
            name="amount"
            label="Số tiền (VND)"
            rules={[{ required: true, message: "Vui lòng nhập số tiền" }]}
          >
            <InputNumber
              min={1000}
              step={10000}
              style={{ width: "100%", borderRadius: 10 }}
              formatter={(v) => `${v}`.replace(/\B(?=(\d{3})+(?!\d))/g, ",")}
              parser={(v: string | undefined) => Number(v?.replace(/,/g, "") ?? 0)}
            />
          </Form.Item>
          <Form.Item name="note" label="Ghi chú">
            <Input.TextArea rows={2} placeholder="Lý do nạp tiền (không bắt buộc)" style={{ borderRadius: 10 }} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}
