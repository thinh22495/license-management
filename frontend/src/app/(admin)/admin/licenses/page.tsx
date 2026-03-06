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
  PlusOutlined,
  GiftOutlined,
} from "@ant-design/icons";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { licensesApi } from "@/lib/api/licenses.api";
import { productsApi } from "@/lib/api/products.api";
import { plansApi } from "@/lib/api/plans.api";
import { usersApi } from "@/lib/api/users.api";
import { formatDate, formatVND } from "@/lib/utils/format";
import type { License, LicensePlan, UserDto, Product } from "@/types";

const { Title } = Typography;
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

  // Create license modal
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
          <code style={{ fontSize: 12 }}>{v}</code>
          <Tooltip title="Copy">
            <Button size="small" type="text" icon={<CopyOutlined />} onClick={() => { navigator.clipboard.writeText(v); message.success("Đã copy"); }} />
          </Tooltip>
        </Space>
      ),
    },
    {
      title: "Email",
      dataIndex: "userEmail",
      key: "userEmail",
      render: (v: string) => v || <Tag color="gold">Chưa gán</Tag>,
    },
    { title: "Sản phẩm", dataIndex: "productName", key: "productName" },
    { title: "Gói", dataIndex: "planName", key: "planName" },
    {
      title: "Trạng thái",
      dataIndex: "status",
      key: "status",
      render: (v: string) => <Tag color={statusColors[v] || "default"}>{v}</Tag>,
    },
    {
      title: "Hết hạn",
      dataIndex: "expiresAt",
      key: "expiresAt",
      render: (v?: string) => v ? formatDate(v) : "Vĩnh viễn",
    },
    {
      title: "Kích hoạt",
      key: "activations",
      render: (_: unknown, r: License) => `${r.currentActivations}/${r.maxActivations}`,
    },
    {
      title: "Thao tác",
      key: "actions",
      render: (_: unknown, record: License) => (
        <Space>
          {record.status === "Active" && (
            <>
              <Popconfirm title="Tạm dừng license?" onConfirm={() => suspend.mutate(record.id)}>
                <Button size="small" icon={<PauseCircleOutlined />}>Tạm dừng</Button>
              </Popconfirm>
              <Popconfirm title="Thu hồi license?" onConfirm={() => revoke.mutate(record.id)}>
                <Button size="small" danger icon={<StopOutlined />}>Thu hồi</Button>
              </Popconfirm>
            </>
          )}
          {(record.status === "Suspended" || record.status === "Revoked") && (
            <Popconfirm title="Khôi phục license?" onConfirm={() => reinstate.mutate(record.id)}>
              <Button size="small" type="primary" icon={<PlayCircleOutlined />}>Khôi phục</Button>
            </Popconfirm>
          )}
        </Space>
      ),
    },
  ];

  return (
    <div>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 16 }}>
        <Title level={3} style={{ margin: 0 }}>Quản lý Licenses</Title>
        <Button type="primary" icon={<GiftOutlined />} onClick={() => setCreateOpen(true)}>
          Tạo / Tặng Key
        </Button>
      </div>

      <Card>
        <Space style={{ marginBottom: 16 }}>
          <Search
            placeholder="Tìm theo key hoặc email..."
            allowClear
            onSearch={(v) => { setSearch(v); setPage(1); }}
            style={{ width: 300 }}
          />
          <Select
            placeholder="Trạng thái"
            allowClear
            onChange={(v) => { setStatusFilter(v); setPage(1); }}
            style={{ width: 150 }}
            options={[
              { value: "Active", label: "Active" },
              { value: "Expired", label: "Expired" },
              { value: "Revoked", label: "Revoked" },
              { value: "Suspended", label: "Suspended" },
              { value: "Pending", label: "Pending" },
            ]}
          />
        </Space>

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
        title="Tạo / Tặng License Key"
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
            <Input.TextArea rows={2} placeholder="Lý do tạo/tặng key (không bắt buộc)" />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}
