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
} from "antd";
import {
  StopOutlined,
  PauseCircleOutlined,
  PlayCircleOutlined,
  CopyOutlined,
} from "@ant-design/icons";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { licensesApi } from "@/lib/api/licenses.api";
import { formatDate } from "@/lib/utils/format";
import type { License } from "@/types";

const { Title } = Typography;
const { Search } = Input;

const statusColors: Record<string, string> = {
  Active: "green",
  Expired: "orange",
  Revoked: "red",
  Suspended: "volcano",
};

export default function AdminLicensesPage() {
  const queryClient = useQueryClient();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<string | undefined>();

  const { data, isLoading } = useQuery({
    queryKey: ["admin-licenses", page, search, statusFilter],
    queryFn: () => licensesApi.getAll({ page, pageSize: 20, search: search || undefined, status: statusFilter }),
    select: (res) => res.data,
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
    { title: "Email", dataIndex: "userEmail", key: "userEmail" },
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
      <Title level={3}>Quản lý Licenses</Title>

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
    </div>
  );
}
