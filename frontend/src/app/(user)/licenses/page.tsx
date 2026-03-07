"use client";

import { useState } from "react";
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
  Modal,
  Input,
} from "antd";
import {
  CopyOutlined,
  ReloadOutlined,
  KeyOutlined,
  DesktopOutlined,
  DeleteOutlined,
  EnterOutlined,
  PlusOutlined,
} from "@ant-design/icons";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { licensesApi } from "@/lib/api/licenses.api";
import { formatDate } from "@/lib/utils/format";
import type { License, LicenseActivation } from "@/types";
import { useRouter } from "next/navigation";

const { Title, Text } = Typography;

const statusColors: Record<string, string> = {
  Active: "green",
  Expired: "orange",
  Revoked: "red",
  Suspended: "volcano",
  Pending: "gold",
};

export default function MyLicensesPage() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const [redeemOpen, setRedeemOpen] = useState(false);
  const [redeemKey, setRedeemKey] = useState("");

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

  const redeem = useMutation({
    mutationFn: (licenseKey: string) => licensesApi.redeem(licenseKey),
    onSuccess: () => {
      message.success("Nhập key thành công!");
      setRedeemOpen(false);
      setRedeemKey("");
      queryClient.invalidateQueries({ queryKey: ["my-licenses"] });
    },
    onError: (err: any) => {
      message.error(err.response?.data?.message || "Nhập key thất bại");
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
          <code style={{
            fontSize: 12,
            background: "#f5f3ff",
            padding: "2px 8px",
            borderRadius: 6,
            color: "#4f46e5",
          }}>{v}</code>
          <Tooltip title="Copy">
            <Button
              size="small"
              type="text"
              icon={<CopyOutlined />}
              onClick={() => { navigator.clipboard.writeText(v); message.success("Đã copy!"); }}
              style={{ color: "#4f46e5" }}
            />
          </Tooltip>
        </Space>
      ),
    },
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
      render: (v?: string) => {
        if (!v) return <Tag color="blue" style={{ borderRadius: 6 }}>Vĩnh viễn</Tag>;
        const isExpired = new Date(v) < new Date();
        return (
          <Text type={isExpired ? "danger" : undefined} strong={isExpired}>
            {formatDate(v)}
          </Text>
        );
      },
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
          {(record.status === "Active" || record.status === "Expired") && record.expiresAt && (
            <Popconfirm
              title="Gia hạn license này?"
              description="Số dư sẽ bị trừ theo giá gói license."
              onConfirm={() => renew.mutate(record.id)}
            >
              <Button size="small" icon={<ReloadOutlined />} loading={renew.isPending} style={{ borderRadius: 8 }}>
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
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 20 }}>
        <Title level={3} className="page-title" style={{ margin: 0 }}>
          <KeyOutlined style={{ marginRight: 10 }} />
          License của tôi
        </Title>
        <Space>
          <Button icon={<EnterOutlined />} onClick={() => setRedeemOpen(true)} style={{ borderRadius: 10 }}>
            Nhập key
          </Button>
          <Button type="primary" icon={<PlusOutlined />} onClick={() => router.push("/products")} className="btn-gradient" style={{ borderRadius: 10 }}>
            Mua thêm license
          </Button>
        </Space>
      </div>

      <Card className="enhanced-card" styles={{ body: { padding: 0 } }}>
        {licenses.length === 0 && !isLoading ? (
          <div style={{ padding: 48 }}>
            <Empty
              description="Bạn chưa có license nào"
              image={Empty.PRESENTED_IMAGE_SIMPLE}
            >
              <Space>
                <Button onClick={() => setRedeemOpen(true)} style={{ borderRadius: 10 }}>
                  Nhập key
                </Button>
                <Button type="primary" onClick={() => router.push("/products")} className="btn-gradient" style={{ borderRadius: 10 }}>
                  Mua license ngay
                </Button>
              </Space>
            </Empty>
          </div>
        ) : (
          <Table
            columns={columns}
            dataSource={licenses}
            rowKey="id"
            loading={isLoading}
            pagination={{ pageSize: 10 }}
            expandable={{
              expandedRowRender: (record) => <ActivationsPanel license={record} />,
              rowExpandable: (record) => record.currentActivations > 0,
            }}
          />
        )}
      </Card>

      <Modal
        title={<span style={{ fontWeight: 700 }}>Nhập License Key</span>}
        open={redeemOpen}
        onCancel={() => { setRedeemOpen(false); setRedeemKey(""); }}
        onOk={() => redeemKey.trim() && redeem.mutate(redeemKey.trim())}
        okText="Nhập key"
        cancelText="Hủy"
        confirmLoading={redeem.isPending}
        okButtonProps={{ disabled: !redeemKey.trim() }}
      >
        <div style={{ marginBottom: 12 }}>
          <Text type="secondary">
            Nhập license key bạn nhận được từ admin hoặc nguồn khác để thêm vào tài khoản.
          </Text>
        </div>
        <Input
          placeholder="VD: LM-XXXXXXXXXXXXXXXXXXXXXX"
          value={redeemKey}
          onChange={(e) => setRedeemKey(e.target.value)}
          onPressEnter={() => redeemKey.trim() && redeem.mutate(redeemKey.trim())}
          size="large"
          autoFocus
          style={{ borderRadius: 10 }}
        />
      </Modal>
    </div>
  );
}

function ActivationsPanel({ license }: { license: License }) {
  const queryClient = useQueryClient();

  const { data: activationsRes, isLoading } = useQuery({
    queryKey: ["activations", license.id],
    queryFn: () => licensesApi.getActivations(license.id),
    select: (res) => res.data.data,
  });

  const deactivate = useMutation({
    mutationFn: (activationId: string) => licensesApi.remoteDeactivate(license.id, activationId),
    onSuccess: () => {
      message.success("Đã hủy kích hoạt thiết bị!");
      queryClient.invalidateQueries({ queryKey: ["activations", license.id] });
      queryClient.invalidateQueries({ queryKey: ["my-licenses"] });
    },
    onError: (err: any) => {
      message.error(err.response?.data?.message || "Hủy kích hoạt thất bại");
    },
  });

  const activations = activationsRes ?? [];

  const activationColumns = [
    {
      title: "Thiết bị",
      key: "device",
      render: (_: unknown, r: LicenseActivation) => (
        <Space>
          <DesktopOutlined style={{ color: "#4f46e5" }} />
          <Text strong>{r.machineName || "Không rõ"}</Text>
        </Space>
      ),
    },
    {
      title: "Hardware ID",
      dataIndex: "hardwareId",
      key: "hardwareId",
      render: (v: string) => (
        <Tooltip title={v}>
          <code style={{ fontSize: 11, background: "#f5f3ff", padding: "2px 6px", borderRadius: 4, color: "#4f46e5" }}>
            {v.substring(0, 16)}...
          </code>
        </Tooltip>
      ),
    },
    {
      title: "IP",
      dataIndex: "ipAddress",
      key: "ipAddress",
      render: (v?: string) => v || "-",
    },
    {
      title: "Kích hoạt lúc",
      dataIndex: "activatedAt",
      key: "activatedAt",
      render: (v: string) => formatDate(v),
    },
    {
      title: "Hoạt động lần cuối",
      dataIndex: "lastSeenAt",
      key: "lastSeenAt",
      render: (v: string) => formatDate(v),
    },
    {
      title: "Trạng thái",
      dataIndex: "isActive",
      key: "isActive",
      render: (v: boolean) => (
        <Tag color={v ? "green" : "default"} style={{ borderRadius: 6 }}>
          {v ? "Đang hoạt động" : "Đã hủy"}
        </Tag>
      ),
    },
    {
      title: "",
      key: "actions",
      render: (_: unknown, r: LicenseActivation) =>
        r.isActive ? (
          <Popconfirm
            title="Hủy kích hoạt thiết bị này?"
            description="Thiết bị sẽ không thể sử dụng license cho đến khi kích hoạt lại."
            onConfirm={() => deactivate.mutate(r.id)}
          >
            <Button size="small" danger icon={<DeleteOutlined />} loading={deactivate.isPending} style={{ borderRadius: 8 }}>
              Hủy
            </Button>
          </Popconfirm>
        ) : null,
    },
  ];

  return (
    <div style={{ padding: "12px 0" }}>
      <Text strong style={{ marginBottom: 12, display: "block", color: "#4f46e5" }}>
        <DesktopOutlined style={{ marginRight: 6 }} />
        Thiết bị đã kích hoạt ({activations.filter((a) => a.isActive).length}/{license.maxActivations})
      </Text>
      <Table
        columns={activationColumns}
        dataSource={activations}
        rowKey="id"
        loading={isLoading}
        pagination={false}
        size="small"
      />
    </div>
  );
}
