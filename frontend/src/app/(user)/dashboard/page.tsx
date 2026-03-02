"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { Card, Typography, Row, Col, Statistic, Button } from "antd";
import {
  KeyOutlined,
  WalletOutlined,
  ShoppingCartOutlined,
  BellOutlined,
} from "@ant-design/icons";
import { useAuthStore } from "@/lib/stores/auth.store";
import { formatVND } from "@/lib/utils/format";

const { Title, Text } = Typography;

export default function DashboardPage() {
  const router = useRouter();
  const { user, isAuthenticated } = useAuthStore();

  useEffect(() => {
    if (!isAuthenticated) {
      router.push("/login");
    }
  }, [isAuthenticated, router]);

  if (!user) return null;

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: "0 auto" }}>
      <div style={{ marginBottom: 24 }}>
        <Title level={3}>
          Xin chào, {user.fullName}!
        </Title>
        <Text type="secondary">
          Dashboard quản lý license của bạn
        </Text>
      </div>

      <Row gutter={[16, 16]}>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title="Số dư tài khoản"
              value={user.balance}
              formatter={(value) => formatVND(value as number)}
              prefix={<WalletOutlined />}
            />
            <Button type="link" style={{ padding: 0, marginTop: 8 }}>
              Nạp tiền
            </Button>
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title="License đang hoạt động"
              value={0}
              prefix={<KeyOutlined />}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title="Sản phẩm có sẵn"
              value={0}
              prefix={<ShoppingCartOutlined />}
            />
            <Button type="link" style={{ padding: 0, marginTop: 8 }}>
              Mua license
            </Button>
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title="Thông báo mới"
              value={0}
              prefix={<BellOutlined />}
            />
          </Card>
        </Col>
      </Row>
    </div>
  );
}
