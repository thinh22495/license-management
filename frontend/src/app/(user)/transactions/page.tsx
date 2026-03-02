"use client";

import { useState } from "react";
import { Card, Table, Tag, Typography, Empty } from "antd";
import { useQuery } from "@tanstack/react-query";
import { paymentsApi, type TransactionDto } from "@/lib/api/payments.api";
import { formatVND, formatDate } from "@/lib/utils/format";

const { Title } = Typography;

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
        return <Tag color={info.color}>{info.label}</Tag>;
      },
    },
    {
      title: "Số tiền",
      dataIndex: "amount",
      key: "amount",
      render: (v: number, r: TransactionDto) => (
        <span style={{ color: r.type === "TopUp" || r.type === "Refund" ? "#52c41a" : "#ff4d4f" }}>
          {r.type === "TopUp" || r.type === "Refund" ? "+" : "-"}{formatVND(v)}
        </span>
      ),
    },
    {
      title: "Số dư trước",
      dataIndex: "balanceBefore",
      key: "balanceBefore",
      render: (v: number) => formatVND(v),
    },
    {
      title: "Số dư sau",
      dataIndex: "balanceAfter",
      key: "balanceAfter",
      render: (v: number) => formatVND(v),
    },
    {
      title: "Phương thức",
      dataIndex: "paymentMethod",
      key: "paymentMethod",
    },
    {
      title: "Trạng thái",
      dataIndex: "status",
      key: "status",
      render: (v: string) => (
        <Tag color={v === "Completed" ? "green" : v === "Pending" ? "orange" : "red"}>
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
      <Title level={3}>Lịch sử giao dịch</Title>

      <Card>
        {!isLoading && (data?.items?.length ?? 0) === 0 ? (
          <Empty description="Chưa có giao dịch nào" image={Empty.PRESENTED_IMAGE_SIMPLE} />
        ) : (
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
              showTotal: (total) => `Tổng ${total} giao dịch`,
            }}
          />
        )}
      </Card>
    </div>
  );
}
