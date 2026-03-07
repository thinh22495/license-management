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
    refetchInterval: 60_000,
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
    const eventSource = new EventSource(`${apiUrl}/notifications/stream`);

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
    <div style={{ width: 380, maxHeight: 440, overflow: "auto" }}>
      <div style={{
        display: "flex",
        justifyContent: "space-between",
        alignItems: "center",
        padding: "10px 4px 12px",
        borderBottom: "1px solid #f0f0f0",
      }}>
        <Text strong style={{ fontSize: 15 }}>Thông báo</Text>
        {(unreadCount ?? 0) > 0 && (
          <Button size="small" type="link" icon={<CheckOutlined />} onClick={() => markAllRead.mutate()}>
            Đánh dấu tất cả đã đọc
          </Button>
        )}
      </div>
      {!notifications?.length && !isLoading ? (
        <Empty description="Không có thông báo" image={Empty.PRESENTED_IMAGE_SIMPLE} style={{ padding: 32 }} />
      ) : (
        <Spin spinning={isLoading}>
          {notifications?.map((item: NotificationDto) => (
            <div
              key={item.id}
              style={{
                padding: "10px 8px",
                background: item.isRead ? "transparent" : "#f5f3ff",
                cursor: item.isRead ? "default" : "pointer",
                borderBottom: "1px solid #f5f5f5",
                borderRadius: 8,
                margin: "4px 0",
                transition: "background 0.2s",
              }}
              onClick={() => { if (!item.isRead) markOneRead.mutate(item.id); }}
            >
              <div style={{ display: "flex", alignItems: "center", gap: 8, marginBottom: 4 }}>
                <Tag color={typeColors[item.type] || "default"} style={{ margin: 0, borderRadius: 6, fontSize: 11 }}>
                  {item.type}
                </Tag>
                <Text strong={!item.isRead} style={{ fontSize: 13, flex: 1 }}>{item.title}</Text>
              </div>
              <Text style={{ fontSize: 12, color: "#6b7280", display: "block", marginBottom: 2 }}>{item.body}</Text>
              <Text type="secondary" style={{ fontSize: 11 }}>{timeAgo(item.createdAt)}</Text>
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
        <Button
          type="text"
          icon={<BellOutlined style={{ fontSize: 20 }} />}
          style={{
            width: 40,
            height: 40,
            borderRadius: 10,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
          }}
        />
      </Badge>
    </Popover>
  );
}
