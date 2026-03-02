"use client";

import {
  Card,
  Table,
  Tag,
  Space,
  Button,
  Typography,
  message,
  Tooltip,
  Empty,
  Popconfirm,
} from "antd";
import {
  CopyOutlined,
  ReloadOutlined,
  KeyOutlined,
} from "@ant-design/icons";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { licensesApi } from "@/lib/api/licenses.api";
import { formatDate } from "@/lib/utils/format";
import type { License } from "@/types";
import { useRouter } from "next/navigation";

const { Title, Text } = Typography;

const statusColors: Record<string, string> = {
  Active: "green",
  Expired: "orange",
  Revoked: "red",
  Suspended: "volcano",
};

export default function MyLicensesPage() {
  const router = useRouter();
  const queryClient = useQueryClient();

  const { data: licensesRes, isLoading } = useQuery({
    queryKey: ["my-licenses"],
    queryFn: () => licensesApi.getMyLicenses(),
    select: (res) => res.data.data,
  });

  const renew = useMutation({
    mutationFn: (licenseId: string) => licensesApi.renew(licenseId),
    onSuccess: () => {
      message.success("Gia hạn thành công!");
      queryClient.invalidateQueries({ queryKey: ["my-licenses"] });
    },
    onError: (err: any) => {
      message.error(err.response?.data?.message || "Gia hạn thất bại");
    },
  });

  const licenses = licensesRes ?? [];

  const columns = [
    {
      title: "Sản phẩm",
      dataIndex: "productName",
      key: "productName",
      render: (v: string, r: License) => (
        <div>
          <Text strong>{v}</Text>
          <br />
          <Text type="secondary" style={{ fontSize: 12 }}>{r.planName}</Text>
        </div>
      ),
    },
    {
      title: "License Key",
      dataIndex: "licenseKey",
      key: "licenseKey",
      render: (v: string) => (
        <Space>
          <code style={{ fontSize: 12 }}>{v}</code>
          <Tooltip title="Copy">
            <Button
              size="small"
              type="text"
              icon={<CopyOutlined />}
              onClick={() => { navigator.clipboard.writeText(v); message.success("Đã copy!"); }}
            />
          </Tooltip>
        </Space>
      ),
    },
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
      render: (v?: string) => {
        if (!v) return <Tag color="blue">Vĩnh viễn</Tag>;
        const isExpired = new Date(v) < new Date();
        return (
          <Text type={isExpired ? "danger" : undefined}>
            {formatDate(v)}
          </Text>
        );
      },
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
          {(record.status === "Active" || record.status === "Expired") && record.expiresAt && (
            <Popconfirm
              title="Gia hạn license này?"
              description="Số dư sẽ bị trừ theo giá gói license."
              onConfirm={() => renew.mutate(record.id)}
            >
              <Button size="small" icon={<ReloadOutlined />} loading={renew.isPending}>
                Gia hạn
              </Button>
            </Popconfirm>
          )}
        </Space>
      ),
    },
  ];

  return (
    <div>
      <div style={{ display: "flex", justifyContent: "space-between", marginBottom: 16 }}>
        <Title level={3} style={{ margin: 0 }}>License của tôi</Title>
        <Button type="primary" icon={<KeyOutlined />} onClick={() => router.push("/products")}>
          Mua thêm license
        </Button>
      </div>

      <Card>
        {licenses.length === 0 && !isLoading ? (
          <Empty
            description="Bạn chưa có license nào"
            image={Empty.PRESENTED_IMAGE_SIMPLE}
          >
            <Button type="primary" onClick={() => router.push("/products")}>
              Mua license ngay
            </Button>
          </Empty>
        ) : (
          <Table
            columns={columns}
            dataSource={licenses}
            rowKey="id"
            loading={isLoading}
            pagination={{ pageSize: 10 }}
          />
        )}
      </Card>
    </div>
  );
}
