"use client";

import { useRouter } from "next/navigation";
import {
  Card,
  Typography,
  Row,
  Col,
  Statistic,
  Button,
  Spin,
  Tag,
  Space,
  Empty,
  Tooltip,
} from "antd";
import {
  KeyOutlined,
  WalletOutlined,
  ShoppingCartOutlined,
  BellOutlined,
  CopyOutlined,
  ArrowRightOutlined,
  AppstoreOutlined,
} from "@ant-design/icons";
import { useQuery } from "@tanstack/react-query";
import { useAuthStore } from "@/lib/stores/auth.store";
import { formatVND } from "@/lib/utils/format";
import { licensesApi } from "@/lib/api/licenses.api";
import { productsApi } from "@/lib/api/products.api";
import { notificationsApi } from "@/lib/api/notifications.api";
import type { License, Product } from "@/types";
import { App } from "antd";

const { Title, Text, Paragraph } = Typography;

const statusColors: Record<string, string> = {
  Active: "green",
  Expired: "red",
  Suspended: "orange",
  Revoked: "default",
};

export default function DashboardPage() {
  const { message } = App.useApp();
  const router = useRouter();
  const { user } = useAuthStore();

  const { data: licenses, isLoading: licensesLoading } = useQuery({
    queryKey: ["my-licenses"],
    queryFn: () => licensesApi.getMyLicenses(),
    select: (res) => (res.data as any)?.data ?? [],
  });

  const { data: products, isLoading: productsLoading } = useQuery({
    queryKey: ["products"],
    queryFn: () => productsApi.getAll({ activeOnly: true }),
    select: (res) => (res.data as any)?.items ?? (res.data as any)?.data ?? [],
  });

  const { data: unreadCount } = useQuery({
    queryKey: ["notifications-unread-count"],
    queryFn: () => notificationsApi.getUnreadCount(),
    select: (res) => res.data.data ?? 0,
  });

  if (!user) return null;

  const activeLicenses = (licenses ?? []).filter((l: License) => l.status === "Active");
  const productCount = (products ?? []).length;

  const copyKey = (key: string) => {
    navigator.clipboard.writeText(key);
    message.success("Đã sao chép license key");
  };

  const statCards = [
    {
      title: "Số dư tài khoản",
      value: user.balance,
      formatter: (v: number) => formatVND(v),
      icon: <WalletOutlined style={{ fontSize: 28, color: "#fff" }} />,
      className: "stat-card stat-card-green",
      link: "/topup",
      linkText: "Nạp tiền",
    },
    {
      title: "License hoạt động",
      value: licensesLoading ? 0 : activeLicenses.length,
      icon: <KeyOutlined style={{ fontSize: 28, color: "#fff" }} />,
      className: "stat-card stat-card-blue",
      link: "/licenses",
      linkText: "Xem chi tiết",
    },
    {
      title: "Sản phẩm có sẵn",
      value: productsLoading ? 0 : productCount,
      icon: <ShoppingCartOutlined style={{ fontSize: 28, color: "#fff" }} />,
      className: "stat-card stat-card-purple",
      link: "/products",
      linkText: "Mua license",
    },
    {
      title: "Thông báo mới",
      value: unreadCount ?? 0,
      icon: <BellOutlined style={{ fontSize: 28, color: "#fff" }} />,
      className: "stat-card stat-card-orange",
      link: "/notifications",
      linkText: "Xem thông báo",
    },
  ];

  return (
    <div>
      <div style={{ marginBottom: 28 }}>
        <Title level={3} className="page-title">
          Xin chào, {user.fullName}!
        </Title>
        <Text type="secondary">Dashboard quản lý license của bạn</Text>
      </div>

      <Row gutter={[20, 20]}>
        {statCards.map((card, index) => (
          <Col xs={24} sm={12} lg={6} key={index}>
            <Card
              hoverable
              onClick={() => router.push(card.link)}
              className={`${card.className} stagger-item animate-fade-in-up`}
              styles={{ body: { padding: 24, position: "relative", zIndex: 1 } }}
            >
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", marginBottom: 16 }}>
                <div>
                  <Text style={{ color: "rgba(255,255,255,0.8)", fontSize: 13 }}>{card.title}</Text>
                  <div style={{ fontSize: 28, fontWeight: 700, color: "#fff", marginTop: 4, lineHeight: 1.2 }}>
                    {card.formatter ? card.formatter(card.value as number) : card.value}
                  </div>
                </div>
                <div style={{
                  width: 52,
                  height: 52,
                  borderRadius: 14,
                  background: "rgba(255,255,255,0.15)",
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "center",
                }}>
                  {card.icon}
                </div>
              </div>
              <Button
                type="link"
                style={{ padding: 0, color: "rgba(255,255,255,0.9)", fontWeight: 500, fontSize: 13 }}
                icon={<ArrowRightOutlined />}
              >
                {card.linkText}
              </Button>
            </Card>
          </Col>
        ))}
      </Row>

      {/* Khu vực phần mềm đang sử dụng */}
      <div style={{ marginTop: 36 }}>
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 20 }}>
          <Title level={4} style={{ margin: 0, fontWeight: 700 }}>
            <AppstoreOutlined style={{ marginRight: 10, color: "#4f46e5" }} />
            Phần mềm đang sử dụng
          </Title>
          <Button type="link" onClick={() => router.push("/licenses")} style={{ fontWeight: 500 }}>
            Xem tất cả <ArrowRightOutlined />
          </Button>
        </div>

        {licensesLoading ? (
          <div style={{ textAlign: "center", padding: 48 }}><Spin size="large" /></div>
        ) : activeLicenses.length === 0 ? (
          <Card className="enhanced-card">
            <Empty
              description="Bạn chưa có license nào đang hoạt động"
              image={Empty.PRESENTED_IMAGE_SIMPLE}
            >
              <Button type="primary" onClick={() => router.push("/products")} className="btn-gradient">
                Mua license ngay
              </Button>
            </Empty>
          </Card>
        ) : (
          <Row gutter={[20, 20]}>
            {activeLicenses.map((license: License, index: number) => (
              <Col xs={24} sm={12} lg={8} key={license.id}>
                <Card
                  hoverable
                  className="license-card stagger-item animate-fade-in-up"
                  styles={{ body: { padding: 24 } }}
                >
                  <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", marginBottom: 16 }}>
                    <div>
                      <Text strong style={{ fontSize: 17, display: "block" }}>{license.productName}</Text>
                      <Text type="secondary" style={{ fontSize: 13 }}>{license.planName}</Text>
                    </div>
                    <Tag color={statusColors[license.status]} style={{ borderRadius: 6, fontWeight: 500 }}>{license.status}</Tag>
                  </div>

                  <div style={{
                    background: "linear-gradient(135deg, #f5f3ff 0%, #eef2ff 100%)",
                    borderRadius: 10,
                    padding: "10px 14px",
                    marginBottom: 16,
                  }}>
                    <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                      <Text code style={{ fontSize: 12, background: "transparent" }}>
                        {license.licenseKey.substring(0, 20)}...
                      </Text>
                      <Tooltip title="Sao chép key">
                        <Button
                          type="text"
                          size="small"
                          icon={<CopyOutlined />}
                          onClick={() => copyKey(license.licenseKey)}
                          style={{ color: "#4f46e5" }}
                        />
                      </Tooltip>
                    </div>
                  </div>

                  <Space orientation="vertical" size={6} style={{ width: "100%" }}>
                    <div style={{ display: "flex", justifyContent: "space-between" }}>
                      <Text type="secondary" style={{ fontSize: 13 }}>Kích hoạt:</Text>
                      <Text strong style={{ fontSize: 13 }}>{license.currentActivations}/{license.maxActivations}</Text>
                    </div>
                    {license.expiresAt && (
                      <div style={{ display: "flex", justifyContent: "space-between" }}>
                        <Text type="secondary" style={{ fontSize: 13 }}>Hết hạn:</Text>
                        <Text strong style={{ fontSize: 13 }}>
                          {new Date(license.expiresAt).toLocaleDateString("vi-VN")}
                        </Text>
                      </div>
                    )}
                  </Space>
                </Card>
              </Col>
            ))}
          </Row>
        )}
      </div>
    </div>
  );
}
