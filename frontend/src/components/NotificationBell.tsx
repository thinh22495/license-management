"use client";

import { useEffect, useRef, useState } from "react";
import { Badge, Button, Popover, Typography, Space, Tag, Empty, Spin } from "antd";
import { BellOutlined, CheckOutlined } from "@ant-design/icons";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { notificationsApi, type NotificationDto } from "@/lib/api/notifications.api";
import { useAuthStore } from "@/lib/stores/auth.store";

const { Text } = Typography;

const typeColors: Record<string, string> = {
  Info: "blue",
  Warning: "orange",
  Success: "green",
  Error: "red",
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

export default function NotificationBell() {
  const { accessToken, isAuthenticated } = useAuthStore();
  const queryClient = useQueryClient();
  const [open, setOpen] = useState(false);
  const eventSourceRef = useRef<EventSource | null>(null);

  const { data: unreadCount } = useQuery({
    queryKey: ["notifications-unread-count"],
    queryFn: () => notificationsApi.getUnreadCount(),
    select: (res) => res.data.data ?? 0,
    refetchInterval: 60_000, // fallback polling every 60s
    enabled: isAuthenticated,
  });

  const { data: notifications, isLoading } = useQuery({
    queryKey: ["notifications-list"],
    queryFn: () => notificationsApi.getAll({ limit: 20 }),
    select: (res) => res.data.data ?? [],
    enabled: open && isAuthenticated,
  });

  const markAllRead = useMutation({
    mutationFn: () => notificationsApi.markRead(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["notifications-unread-count"] });
      queryClient.invalidateQueries({ queryKey: ["notifications-list"] });
    },
  });

  const markOneRead = useMutation({
    mutationFn: (id: string) => notificationsApi.markRead(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["notifications-unread-count"] });
      queryClient.invalidateQueries({ queryKey: ["notifications-list"] });
    },
  });

  // SSE connection for realtime
  useEffect(() => {
    if (!isAuthenticated || !accessToken) return;

    const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000/api/v1";
    const eventSource = new EventSource(`${apiUrl}/notifications/stream`, {
      // Note: EventSource doesn't support custom headers natively
      // In production, use a polyfill or pass token via query param
    });

    eventSource.addEventListener("notification", () => {
      queryClient.invalidateQueries({ queryKey: ["notifications-unread-count"] });
      if (open) {
        queryClient.invalidateQueries({ queryKey: ["notifications-list"] });
      }
    });

    eventSourceRef.current = eventSource;

    return () => {
      eventSource.close();
      eventSourceRef.current = null;
    };
  }, [isAuthenticated, accessToken, queryClient, open]);

  const content = (
    <div style={{ width: 360, maxHeight: 400, overflow: "auto" }}>
      <div style={{ display: "flex", justifyContent: "space-between", padding: "8px 0", borderBottom: "1px solid #f0f0f0" }}>
        <Text strong>Thông báo</Text>
        {(unreadCount ?? 0) > 0 && (
          <Button size="small" type="link" icon={<CheckOutlined />} onClick={() => markAllRead.mutate()}>
            Đánh dấu tất cả đã đọc
          </Button>
        )}
      </div>
      {!notifications?.length && !isLoading ? (
        <Empty description="Không có thông báo" image={Empty.PRESENTED_IMAGE_SIMPLE} style={{ padding: 24 }} />
      ) : (
        <Spin spinning={isLoading}>
          {notifications?.map((item: NotificationDto) => (
            <div
              key={item.id}
              style={{
                padding: "8px 4px",
                background: item.isRead ? "transparent" : "#f6ffed",
                cursor: item.isRead ? "default" : "pointer",
                borderBottom: "1px solid #f0f0f0",
              }}
              onClick={() => { if (!item.isRead) markOneRead.mutate(item.id); }}
            >
              <Space>
                <Tag color={typeColors[item.type] || "default"} style={{ margin: 0 }}>
                  {item.type}
                </Tag>
                <Text strong={!item.isRead} style={{ fontSize: 13 }}>{item.title}</Text>
              </Space>
              <div>
                <Text style={{ fontSize: 12 }}>{item.body}</Text>
                <br />
                <Text type="secondary" style={{ fontSize: 11 }}>{timeAgo(item.createdAt)}</Text>
              </div>
            </div>
          ))}
        </Spin>
      )}
    </div>
  );

  if (!isAuthenticated) return null;

  return (
    <Popover
      content={content}
      trigger="click"
      open={open}
      onOpenChange={setOpen}
      placement="bottomRight"
    >
      <Badge count={unreadCount ?? 0} size="small" offset={[-2, 2]}>
        <Button type="text" icon={<BellOutlined style={{ fontSize: 18 }} />} />
      </Badge>
    </Popover>
  );
}
