"use client";

import { useState } from "react";
import {
  Typography,
  Card,
  Button,
  Space,
  Tag,
  Empty,
  Spin,
  Popconfirm,
  Tabs,
  Badge,
  message,
} from "antd";
import {
  BellOutlined,
  CheckOutlined,
  DeleteOutlined,
  CheckCircleOutlined,
  InfoCircleOutlined,
  WarningOutlined,
  CloseCircleOutlined,
} from "@ant-design/icons";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { notificationsApi, type NotificationDto } from "@/lib/api/notifications.api";

const { Title, Text, Paragraph } = Typography;

const typeConfig: Record<string, { color: string; icon: React.ReactNode }> = {
  Info: { color: "blue", icon: <InfoCircleOutlined /> },
  Warning: { color: "orange", icon: <WarningOutlined /> },
  Success: { color: "green", icon: <CheckCircleOutlined /> },
  Error: { color: "red", icon: <CloseCircleOutlined /> },
};

function timeAgo(dateStr: string): string {
  const seconds = Math.floor((Date.now() - new Date(dateStr).getTime()) / 1000);
  if (seconds < 60) return "vừa xong";
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes} phút trước`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours} giờ trước`;
  const days = Math.floor(hours / 24);
  return `${days} ngày trước`;
}

export default function NotificationsPage() {
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState<string>("all");

  const { data: notifications, isLoading } = useQuery({
    queryKey: ["notifications-page", activeTab],
    queryFn: () => notificationsApi.getAll({
      unreadOnly: activeTab === "unread",
      limit: 100,
    }),
    select: (res) => res.data.data ?? [],
  });

  const { data: unreadCount } = useQuery({
    queryKey: ["notifications-unread-count"],
    queryFn: () => notificationsApi.getUnreadCount(),
    select: (res) => res.data.data ?? 0,
  });

  const invalidateAll = () => {
    queryClient.invalidateQueries({ queryKey: ["notifications-page"] });
    queryClient.invalidateQueries({ queryKey: ["notifications-unread-count"] });
    queryClient.invalidateQueries({ queryKey: ["notifications-list"] });
  };

  const markAllRead = useMutation({
    mutationFn: () => notificationsApi.markRead(),
    onSuccess: () => {
      message.success("Đã đánh dấu tất cả đã đọc");
      invalidateAll();
    },
  });

  const markOneRead = useMutation({
    mutationFn: (id: string) => notificationsApi.markRead(id),
    onSuccess: () => {
      invalidateAll();
    },
  });

  const deleteNotification = useMutation({
    mutationFn: (id: string) => notificationsApi.delete(id),
    onSuccess: () => {
      message.success("Đã xóa thông báo");
      invalidateAll();
    },
    onError: () => {
      message.error("Không thể xóa thông báo");
    },
  });

  const items = notifications ?? [];

  return (
    <div>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 24 }}>
        <div>
          <Title level={3} style={{ margin: 0 }}>
            <BellOutlined style={{ marginRight: 8 }} />
            Thông báo
          </Title>
          <Text type="secondary">Quản lý tất cả thông báo của bạn</Text>
        </div>
        {(unreadCount ?? 0) > 0 && (
          <Button
            icon={<CheckOutlined />}
            onClick={() => markAllRead.mutate()}
            loading={markAllRead.isPending}
          >
            Đánh dấu tất cả đã đọc
          </Button>
        )}
      </div>

      <Tabs
        activeKey={activeTab}
        onChange={setActiveTab}
        items={[
          {
            key: "all",
            label: "Tất cả",
          },
          {
            key: "unread",
            label: (
              <Badge count={unreadCount ?? 0} size="small" offset={[10, 0]}>
                Chưa đọc
              </Badge>
            ),
          },
        ]}
      />

      {isLoading ? (
        <div style={{ textAlign: "center", padding: 48 }}><Spin size="large" /></div>
      ) : items.length === 0 ? (
        <Card>
          <Empty
            description={activeTab === "unread" ? "Không có thông báo chưa đọc" : "Không có thông báo nào"}
            image={Empty.PRESENTED_IMAGE_SIMPLE}
          />
        </Card>
      ) : (
        <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
          {items.map((item: NotificationDto) => {
            const config = typeConfig[item.type] || typeConfig.Info;
            return (
              <Card
                key={item.id}
                size="small"
                style={{
                  borderLeft: `3px solid ${item.isRead ? "#f0f0f0" : config.color === "blue" ? "#1677ff" : config.color === "orange" ? "#fa8c16" : config.color === "green" ? "#52c41a" : "#ff4d4f"}`,
                  background: item.isRead ? "#fff" : "#fafafa",
                }}
                styles={{ body: { padding: "12px 16px" } }}
              >
                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start" }}>
                  <div style={{ flex: 1 }}>
                    <div style={{ display: "flex", alignItems: "center", gap: 8, marginBottom: 4 }}>
                      <Tag
                        icon={config.icon}
                        color={config.color}
                        style={{ margin: 0 }}
                      >
                        {item.type}
                      </Tag>
                      <Text strong={!item.isRead} style={{ fontSize: 15 }}>
                        {item.title}
                      </Text>
                      {!item.isRead && (
                        <Badge status="processing" />
                      )}
                    </div>
                    <Paragraph
                      style={{ margin: "4px 0 0", color: "#595959", fontSize: 13 }}
                      ellipsis={{ rows: 2 }}
                    >
                      {item.body}
                    </Paragraph>
                    <Text type="secondary" style={{ fontSize: 12 }}>
                      {timeAgo(item.createdAt)}
                    </Text>
                  </div>
                  <Space size={4}>
                    {!item.isRead && (
                      <Button
                        type="text"
                        size="small"
                        icon={<CheckOutlined />}
                        onClick={() => markOneRead.mutate(item.id)}
                        title="Đánh dấu đã đọc"
                      />
                    )}
                    <Popconfirm
                      title="Xóa thông báo này?"
                      onConfirm={() => deleteNotification.mutate(item.id)}
                      okText="Xóa"
                      cancelText="Hủy"
                    >
                      <Button
                        type="text"
                        size="small"
                        danger
                        icon={<DeleteOutlined />}
                        title="Xóa"
                      />
                    </Popconfirm>
                  </Space>
                </div>
              </Card>
            );
          })}
        </div>
      )}
    </div>
  );
}
