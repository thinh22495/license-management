"use client";

import { useState } from "react";
import { Card, Table, Tag, Typography } from "antd";
import { HistoryOutlined } from "@ant-design/icons";
import { useQuery } from "@tanstack/react-query";
import { paymentsApi, type TransactionDto } from "@/lib/api/payments.api";
import { formatVND, formatDate } from "@/lib/utils/format";

const { Title, Text } = Typography;

const typeLabels: Record<string, { label: string; color: string }> = {
  TopUp: { label: "Nạp tiền", color: "green" },
  Purchase: { label: "Mua license", color: "blue" },
  Renewal: { label: "Gia hạn", color: "orange" },
  Refund: { label: "Hoàn tiền", color: "purple" },
};

export default function TransactionsPage() {
  const [page, setPage] = useState(1);

  const { data, isLoading } = useQuery({
    queryKey: ["my-transactions", page],
    queryFn: () => paymentsApi.getTransactions({ page, pageSize: 20 }),
    select: (res) => res.data,
  });

  const columns = [
    {
      title: "Loại",
      dataIndex: "type",
      key: "type",
      render: (v: string) => {
        const info = typeLabels[v] || { label: v, color: "default" };
        return <Tag color={info.color} style={{ borderRadius: 6, fontWeight: 500 }}>{info.label}</Tag>;
      },
    },
    {
      title: "Số tiền",
      dataIndex: "amount",
      key: "amount",
      render: (v: number, r: TransactionDto) => (
        <Text strong style={{
          color: r.type === "TopUp" || r.type === "Refund" ? "#10b981" : "#ef4444",
          fontSize: 14,
        }}>
          {r.type === "TopUp" || r.type === "Refund" ? "+" : "-"}{formatVND(v)}
        </Text>
      ),
    },
    {
      title: "Số dư trước",
      dataIndex: "balanceBefore",
      key: "balanceBefore",
      render: (v: number) => <Text type="secondary">{formatVND(v)}</Text>,
    },
    {
      title: "Số dư sau",
      dataIndex: "balanceAfter",
      key: "balanceAfter",
      render: (v: number) => <Text strong>{formatVND(v)}</Text>,
    },
    {
      title: "Phương thức",
      dataIndex: "paymentMethod",
      key: "paymentMethod",
      render: (v: string) => v ? <Tag style={{ borderRadius: 6 }}>{v}</Tag> : "-",
    },
    {
      title: "Trạng thái",
      dataIndex: "status",
      key: "status",
      render: (v: string) => (
        <Tag
          color={v === "Completed" ? "green" : v === "Pending" ? "orange" : "red"}
          style={{ borderRadius: 6, fontWeight: 500 }}
        >
          {v}
        </Tag>
      ),
    },
    {
      title: "Ngày",
      dataIndex: "createdAt",
      key: "createdAt",
      render: (v: string) => formatDate(v),
    },
  ];

  return (
    <div>
      <Title level={3} className="page-title">
        <HistoryOutlined style={{ marginRight: 10 }} />
        Lịch sử giao dịch
      </Title>

      <Card className="enhanced-card" styles={{ body: { padding: 0 } }}>
        <Table
          columns={columns}
          dataSource={data?.items ?? []}
          rowKey="id"
          loading={isLoading}
          locale={{ emptyText: "Chưa có giao dịch nào" }}
          pagination={{
            current: page,
            total: data?.totalCount ?? 0,
            pageSize: 20,
            onChange: setPage,
            showTotal: (total) => `Tổng ${total} giao dịch`,
          }}
        />
      </Card>
    </div>
  );
}
