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
  message,
  Popconfirm,
} from "antd";
import { LockOutlined, UnlockOutlined } from "@ant-design/icons";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { usersApi } from "@/lib/api/users.api";
import { formatVND, formatDate } from "@/lib/utils/format";
import type { UserDto } from "@/types";

const { Title } = Typography;
const { Search } = Input;

export default function AdminUsersPage() {
  const queryClient = useQueryClient();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");

  const { data: usersRes, isLoading } = useQuery({
    queryKey: ["admin-users", page, search],
    queryFn: () => usersApi.getAll({ page, pageSize: 20, search: search || undefined }),
    select: (res) => res.data.data,
  });

  const toggleLock = useMutation({
    mutationFn: ({ id, isLocked }: { id: string; isLocked: boolean }) => usersApi.lock(id, isLocked),
    onSuccess: (_, variables) => {
      message.success(variables.isLocked ? "Đã khóa tài khoản" : "Đã mở khóa tài khoản");
      queryClient.invalidateQueries({ queryKey: ["admin-users"] });
    },
    onError: () => message.error("Lỗi khi thay đổi trạng thái"),
  });

  const columns = [
    { title: "Email", dataIndex: "email", key: "email" },
    { title: "Họ tên", dataIndex: "fullName", key: "fullName" },
    { title: "SĐT", dataIndex: "phone", key: "phone" },
    {
      title: "Role",
      dataIndex: "role",
      key: "role",
      render: (v: string) => <Tag color={v === "Admin" ? "purple" : "blue"}>{v}</Tag>,
    },
    {
      title: "Số dư",
      dataIndex: "balance",
      key: "balance",
      render: (v: number) => formatVND(v),
    },
    {
      title: "Trạng thái",
      key: "status",
      render: (_: unknown, r: UserDto) => (
        <Space>
          {r.isLocked && <Tag color="red">Bị khóa</Tag>}
          {r.emailVerified ? <Tag color="green">Email verified</Tag> : <Tag>Chưa verify</Tag>}
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
          {record.role !== "Admin" && (
            <Popconfirm
              title={record.isLocked ? "Mở khóa tài khoản này?" : "Khóa tài khoản này?"}
              onConfirm={() => toggleLock.mutate({ id: record.id, isLocked: !record.isLocked })}
            >
              <Button
                size="small"
                danger={!record.isLocked}
                icon={record.isLocked ? <UnlockOutlined /> : <LockOutlined />}
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
      <Title level={3}>Quản lý người dùng</Title>

      <Card>
        <Search
          placeholder="Tìm theo email hoặc tên..."
          allowClear
          onSearch={(v) => { setSearch(v); setPage(1); }}
          style={{ width: 300, marginBottom: 16 }}
        />

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
    </div>
  );
}
