"use client";

import { useState } from "react";
import {
  Card,
  Table,
  Tag,
  Space,
  Button,
  Input,
  Select,
  Typography,
  message,
  Popconfirm,
  Tooltip,
  Modal,
  Form,
} from "antd";
import {
  StopOutlined,
  PauseCircleOutlined,
  PlayCircleOutlined,
  CopyOutlined,
  KeyOutlined,
  GiftOutlined,
} from "@ant-design/icons";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { licensesApi } from "@/lib/api/licenses.api";
import { productsApi } from "@/lib/api/products.api";
import { plansApi } from "@/lib/api/plans.api";
import { usersApi } from "@/lib/api/users.api";
import { formatDate, formatVND } from "@/lib/utils/format";
import type { License, LicensePlan, UserDto, Product } from "@/types";

const { Title, Text } = Typography;
const { Search } = Input;

const statusColors: Record<string, string> = {
  Active: "green",
  Expired: "orange",
  Revoked: "red",
  Suspended: "volcano",
  Pending: "gold",
};

export default function AdminLicensesPage() {
  const queryClient = useQueryClient();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<string | undefined>();

  const [createOpen, setCreateOpen] = useState(false);
  const [createForm] = Form.useForm();
  const [selectedProductId, setSelectedProductId] = useState<string | undefined>();
  const [plans, setPlans] = useState<LicensePlan[]>([]);
  const [userSearch, setUserSearch] = useState("");

  const { data, isLoading } = useQuery({
    queryKey: ["admin-licenses", page, search, statusFilter],
    queryFn: () => licensesApi.getAll({ page, pageSize: 20, search: search || undefined, status: statusFilter }),
    select: (res) => res.data,
  });

  const { data: productsRes } = useQuery({
    queryKey: ["products"],
    queryFn: () => productsApi.getAll({ activeOnly: true }),
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    select: (res) => (res.data as any)?.items ?? [],
  });

  const { data: usersRes } = useQuery({
    queryKey: ["admin-users-select", userSearch],
    queryFn: () => usersApi.getAll({ page: 1, pageSize: 50, search: userSearch || undefined }),
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    select: (res) => (res.data as any)?.items ?? [],
    enabled: createOpen,
  });

  const revoke = useMutation({
    mutationFn: (id: string) => licensesApi.revoke(id),
    onSuccess: () => { message.success("Đã thu hồi"); queryClient.invalidateQueries({ queryKey: ["admin-licenses"] }); },
  });

  const suspend = useMutation({
    mutationFn: (id: string) => licensesApi.suspend(id),
    onSuccess: () => { message.success("Đã tạm dừng"); queryClient.invalidateQueries({ queryKey: ["admin-licenses"] }); },
  });

  const reinstate = useMutation({
    mutationFn: (id: string) => licensesApi.reinstate(id),
    onSuccess: () => { message.success("Đã khôi phục"); queryClient.invalidateQueries({ queryKey: ["admin-licenses"] }); },
  });

  const createLicense = useMutation({
    mutationFn: (values: { userId?: string; licensePlanId: string; note?: string }) =>
      licensesApi.adminCreate(values),
    onSuccess: () => {
      message.success("Tạo license thành công");
      queryClient.invalidateQueries({ queryKey: ["admin-licenses"] });
      setCreateOpen(false);
      createForm.resetFields();
      setSelectedProductId(undefined);
      setPlans([]);
    },
    onError: () => message.error("Lỗi khi tạo license"),
  });

  const handleProductChange = async (productId: string) => {
    setSelectedProductId(productId);
    createForm.setFieldValue("licensePlanId", undefined);
    try {
      const res = await plansApi.getByProduct(productId, { activeOnly: true });
      const plansData = res.data;
      setPlans(Array.isArray(plansData) ? plansData : (plansData as any)?.data ?? []);
    } catch {
      setPlans([]);
    }
  };

  const columns = [
    {
      title: "License Key",
      dataIndex: "licenseKey",
      key: "licenseKey",
      render: (v: string) => (
        <Space>
          <code style={{
            fontSize: 12,
            background: "#f5f3ff",
            padding: "2px 8px",
            borderRadius: 6,
            color: "#4f46e5",
          }}>{v}</code>
          <Tooltip title="Copy">
            <Button size="small" type="text" icon={<CopyOutlined />} onClick={() => { navigator.clipboard.writeText(v); message.success("Đã copy"); }} style={{ color: "#4f46e5" }} />
          </Tooltip>
        </Space>
      ),
    },
    {
      title: "Email",
      dataIndex: "userEmail",
      key: "userEmail",
      render: (v: string) => v || <Tag color="gold" style={{ borderRadius: 6 }}>Chưa gán</Tag>,
    },
    { title: "Sản phẩm", dataIndex: "productName", key: "productName", render: (v: string) => <Text strong>{v}</Text> },
    { title: "Gói", dataIndex: "planName", key: "planName" },
    {
      title: "Trạng thái",
      dataIndex: "status",
      key: "status",
      render: (v: string) => <Tag color={statusColors[v] || "default"} style={{ borderRadius: 6, fontWeight: 500 }}>{v}</Tag>,
    },
    {
      title: "Hết hạn",
      dataIndex: "expiresAt",
      key: "expiresAt",
      render: (v?: string) => v ? formatDate(v) : <Tag color="blue" style={{ borderRadius: 6 }}>Vĩnh viễn</Tag>,
    },
    {
      title: "Kích hoạt",
      key: "activations",
      render: (_: unknown, r: License) => (
        <Text>
          <Text strong style={{ color: "#4f46e5" }}>{r.currentActivations}</Text>
          <Text type="secondary">/{r.maxActivations}</Text>
        </Text>
      ),
    },
    {
      title: "Thao tác",
      key: "actions",
      render: (_: unknown, record: License) => (
        <Space>
          {record.status === "Active" && (
            <>
              <Popconfirm title="Tạm dừng license?" onConfirm={() => suspend.mutate(record.id)}>
                <Button size="small" icon={<PauseCircleOutlined />} style={{ borderRadius: 8 }}>Tạm dừng</Button>
              </Popconfirm>
              <Popconfirm title="Thu hồi license?" onConfirm={() => revoke.mutate(record.id)}>
                <Button size="small" danger icon={<StopOutlined />} style={{ borderRadius: 8 }}>Thu hồi</Button>
              </Popconfirm>
            </>
          )}
          {(record.status === "Suspended" || record.status === "Revoked") && (
            <Popconfirm title="Khôi phục license?" onConfirm={() => reinstate.mutate(record.id)}>
              <Button size="small" type="primary" icon={<PlayCircleOutlined />} style={{ borderRadius: 8 }}>Khôi phục</Button>
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
          <KeyOutlined style={{ marginRight: 10 }} />
          Quản lý Licenses
        </Title>
        <Button type="primary" icon={<GiftOutlined />} onClick={() => setCreateOpen(true)} className="btn-gradient" style={{ borderRadius: 10 }}>
          Tạo / Tặng Key
        </Button>
      </div>

      <Card className="enhanced-card" styles={{ body: { padding: "16px 0 0" } }}>
        <div style={{ padding: "0 16px", marginBottom: 16 }}>
          <Space>
            <Search
              placeholder="Tìm theo key hoặc email..."
              allowClear
              onSearch={(v) => { setSearch(v); setPage(1); }}
              style={{ width: 320 }}
            />
            <Select
              placeholder="Trạng thái"
              allowClear
              onChange={(v) => { setStatusFilter(v); setPage(1); }}
              style={{ width: 160 }}
              options={[
                { value: "Active", label: "Active" },
                { value: "Expired", label: "Expired" },
                { value: "Revoked", label: "Revoked" },
                { value: "Suspended", label: "Suspended" },
                { value: "Pending", label: "Pending" },
              ]}
            />
          </Space>
        </div>

        <Table
          columns={columns}
          dataSource={data?.items ?? []}
          rowKey="id"
          loading={isLoading}
          pagination={{
            current: page,
            total: data?.totalCount ?? 0,
            pageSize: 20,
            onChange: setPage,
            showTotal: (total) => `Tổng ${total} licenses`,
          }}
        />
      </Card>

      {/* Create / Gift License Modal */}
      <Modal
        title={<Text strong style={{ fontSize: 16 }}>Tạo / Tặng License Key</Text>}
        open={createOpen}
        onOk={() => createForm.submit()}
        onCancel={() => {
          setCreateOpen(false);
          createForm.resetFields();
          setSelectedProductId(undefined);
          setPlans([]);
        }}
        confirmLoading={createLicense.isPending}
        width={520}
      >
        <Form form={createForm} layout="vertical" onFinish={(v) => createLicense.mutate(v)}>
          <Form.Item
            name="userId"
            label="Người dùng"
            extra="Bỏ trống để tạo key chưa gán (Pending) - user sẽ nhập key sau"
          >
            <Select
              showSearch
              allowClear
              placeholder="Tìm theo email hoặc tên... (để trống = key chưa gán)"
              filterOption={false}
              onSearch={setUserSearch}
              options={(usersRes ?? []).map((u: UserDto) => ({
                value: u.id,
                label: `${u.email} - ${u.fullName}`,
              }))}
            />
          </Form.Item>

          <Form.Item
            name="productId"
            label="Sản phẩm"
            rules={[{ required: true, message: "Vui lòng chọn sản phẩm" }]}
          >
            <Select
              placeholder="Chọn sản phẩm"
              onChange={handleProductChange}
              options={(productsRes ?? []).map((p: Product) => ({
                value: p.id,
                label: p.name,
              }))}
            />
          </Form.Item>

          <Form.Item
            name="licensePlanId"
            label="Gói license"
            rules={[{ required: true, message: "Vui lòng chọn gói" }]}
          >
            <Select
              placeholder={selectedProductId ? "Chọn gói" : "Chọn sản phẩm trước"}
              disabled={!selectedProductId}
              options={plans.map((p) => ({
                value: p.id,
                label: `${p.name} - ${p.durationDays === 0 ? "Vĩnh viễn" : `${p.durationDays} ngày`} - ${formatVND(p.price)}`,
              }))}
            />
          </Form.Item>

          <Form.Item name="note" label="Ghi chú">
            <Input.TextArea rows={2} placeholder="Lý do tạo/tặng key (không bắt buộc)" style={{ borderRadius: 10 }} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}
