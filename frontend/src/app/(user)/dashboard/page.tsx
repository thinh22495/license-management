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
import { message } from "antd";

const { Title, Text, Paragraph } = Typography;

const statusColors: Record<string, string> = {
  Active: "green",
  Expired: "red",
  Suspended: "orange",
  Revoked: "default",
};

export default function DashboardPage() {
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

  return (
    <div>
      <div style={{ marginBottom: 24 }}>
        <Title level={3}>Xin chào, {user.fullName}!</Title>
        <Text type="secondary">Dashboard quản lý license của bạn</Text>
      </div>

      <Row gutter={[16, 16]}>
        <Col xs={24} sm={12} lg={6}>
          <Card hoverable onClick={() => router.push("/topup")} style={{ height: "100%" }}>
            <Statistic
              title="Số dư tài khoản"
              value={user.balance}
              formatter={(value) => formatVND(value as number)}
              prefix={<WalletOutlined style={{ color: "#52c41a" }} />}
            />
            <Button
              type="link"
              style={{ padding: 0, marginTop: 8 }}
              icon={<ArrowRightOutlined />}
            >
              Nạp tiền
            </Button>
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card hoverable onClick={() => router.push("/licenses")} style={{ height: "100%" }}>
            <Statistic
              title="License đang hoạt động"
              value={licensesLoading ? "..." : activeLicenses.length}
              prefix={<KeyOutlined style={{ color: "#1677ff" }} />}
            />
            <Button
              type="link"
              style={{ padding: 0, marginTop: 8 }}
              icon={<ArrowRightOutlined />}
            >
              Xem chi tiết
            </Button>
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card hoverable onClick={() => router.push("/products")} style={{ height: "100%" }}>
            <Statistic
              title="Sản phẩm có sẵn"
              value={productsLoading ? "..." : productCount}
              prefix={<ShoppingCartOutlined style={{ color: "#722ed1" }} />}
            />
            <Button
              type="link"
              style={{ padding: 0, marginTop: 8 }}
              icon={<ArrowRightOutlined />}
            >
              Mua license
            </Button>
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card hoverable onClick={() => router.push("/notifications")} style={{ height: "100%" }}>
            <Statistic
              title="Thông báo mới"
              value={unreadCount ?? 0}
              prefix={<BellOutlined style={{ color: "#fa8c16" }} />}
            />
            <Button
              type="link"
              style={{ padding: 0, marginTop: 8 }}
              icon={<ArrowRightOutlined />}
            >
              Xem thông báo
            </Button>
          </Card>
        </Col>
      </Row>

      {/* Khu vực phần mềm đang sử dụng */}
      <div style={{ marginTop: 32 }}>
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 16 }}>
          <Title level={4} style={{ margin: 0 }}>
            <AppstoreOutlined style={{ marginRight: 8 }} />
            Phần mềm đang sử dụng
          </Title>
          <Button type="link" onClick={() => router.push("/licenses")}>
            Xem tất cả <ArrowRightOutlined />
          </Button>
        </div>

        {licensesLoading ? (
          <div style={{ textAlign: "center", padding: 48 }}><Spin size="large" /></div>
        ) : activeLicenses.length === 0 ? (
          <Card>
            <Empty
              description="Bạn chưa có license nào đang hoạt động"
              image={Empty.PRESENTED_IMAGE_SIMPLE}
            >
              <Button type="primary" onClick={() => router.push("/products")}>
                Mua license ngay
              </Button>
            </Empty>
          </Card>
        ) : (
          <Row gutter={[16, 16]}>
            {activeLicenses.map((license: License) => (
              <Col xs={24} sm={12} lg={8} key={license.id}>
                <Card
                  hoverable
                  style={{ height: "100%" }}
                  styles={{ body: { padding: 20 } }}
                >
                  <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", marginBottom: 12 }}>
                    <div>
                      <Text strong style={{ fontSize: 16 }}>{license.productName}</Text>
                      <br />
                      <Text type="secondary" style={{ fontSize: 13 }}>{license.planName}</Text>
                    </div>
                    <Tag color={statusColors[license.status]}>{license.status}</Tag>
                  </div>

                  <div style={{ background: "#f5f5f5", borderRadius: 6, padding: "8px 12px", marginBottom: 12 }}>
                    <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                      <Text code style={{ fontSize: 12 }}>
                        {license.licenseKey.substring(0, 20)}...
                      </Text>
                      <Tooltip title="Sao chép key">
                        <Button
                          type="text"
                          size="small"
                          icon={<CopyOutlined />}
                          onClick={() => copyKey(license.licenseKey)}
                        />
                      </Tooltip>
                    </div>
                  </div>

                  <Space direction="vertical" size={4} style={{ width: "100%" }}>
                    <div style={{ display: "flex", justifyContent: "space-between" }}>
                      <Text type="secondary" style={{ fontSize: 13 }}>Kích hoạt:</Text>
                      <Text style={{ fontSize: 13 }}>{license.currentActivations}/{license.maxActivations}</Text>
                    </div>
                    {license.expiresAt && (
                      <div style={{ display: "flex", justifyContent: "space-between" }}>
                        <Text type="secondary" style={{ fontSize: 13 }}>Hết hạn:</Text>
                        <Text style={{ fontSize: 13 }}>
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
