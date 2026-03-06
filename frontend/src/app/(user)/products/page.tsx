"use client";

import { useState } from "react";
import {
  Card,
  Row,
  Col,
  Typography,
  Tag,
  Button,
  Modal,
  Space,
  message,
  Spin,
  Empty,
  Divider,
  Progress,
} from "antd";
import {
  ShoppingCartOutlined,
  ClockCircleOutlined,
  TeamOutlined,
  GlobalOutlined,
  CheckCircleOutlined,
  ThunderboltOutlined,
  CrownOutlined,
  SafetyCertificateOutlined,
} from "@ant-design/icons";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { productsApi } from "@/lib/api/products.api";
import { plansApi } from "@/lib/api/plans.api";
import { licensesApi } from "@/lib/api/licenses.api";
import { formatVND } from "@/lib/utils/format";
import { useAuthStore } from "@/lib/stores/auth.store";
import apiClient from "@/lib/api/client";
import type { Product, LicensePlan } from "@/types";

const { Title, Text, Paragraph } = Typography;

const productColors = ["#1677ff", "#722ed1", "#13c2c2", "#eb2f96", "#fa8c16", "#52c41a"];

export default function ProductsPage() {
  const { user } = useAuthStore();
  const [selectedProduct, setSelectedProduct] = useState<Product | null>(null);

  const { data: productsRes, isLoading } = useQuery({
    queryKey: ["products"],
    queryFn: () => productsApi.getAll({ activeOnly: true }),
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    select: (res) => (res.data as any)?.items ?? (res.data as any)?.data ?? [],
  });

  const products: Product[] = productsRes ?? [];

  return (
    <div>
      <div style={{ marginBottom: 24 }}>
        <Title level={3}>
          <ShoppingCartOutlined style={{ marginRight: 8 }} />
          Cửa hàng License
        </Title>
        <Paragraph type="secondary">
          Chọn sản phẩm bạn quan tâm để xem các gói license có sẵn
        </Paragraph>
      </div>

      {isLoading ? (
        <div style={{ textAlign: "center", padding: 48 }}><Spin size="large" /></div>
      ) : products.length === 0 ? (
        <Empty description="Chưa có sản phẩm nào" />
      ) : (
        <Row gutter={[24, 24]}>
          {products.map((product: Product, index: number) => {
            const color = productColors[index % productColors.length];
            return (
              <Col xs={24} sm={12} lg={8} key={product.id}>
                <Card
                  hoverable
                  onClick={() => setSelectedProduct(product)}
                  style={{
                    height: "100%",
                    borderTop: `3px solid ${color}`,
                    overflow: "hidden",
                  }}
                  styles={{ body: { padding: 24, display: "flex", flexDirection: "column", height: "100%" } }}
                >
                  <div style={{
                    width: 56,
                    height: 56,
                    borderRadius: 12,
                    background: `${color}15`,
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                    marginBottom: 16,
                  }}>
                    <SafetyCertificateOutlined style={{ fontSize: 28, color }} />
                  </div>

                  <Title level={4} style={{ margin: "0 0 8px 0" }}>{product.name}</Title>
                  <Paragraph
                    type="secondary"
                    ellipsis={{ rows: 2 }}
                    style={{ flex: 1, marginBottom: 16 }}
                  >
                    {product.description || "Phần mềm chuyên nghiệp"}
                  </Paragraph>

                  {product.websiteUrl && (
                    <div style={{ marginBottom: 12 }}>
                      <Tag icon={<GlobalOutlined />} color="blue">
                        {new URL(product.websiteUrl).hostname}
                      </Tag>
                    </div>
                  )}

                  <Button
                    type="primary"
                    block
                    icon={<ShoppingCartOutlined />}
                    style={{ background: color, borderColor: color }}
                  >
                    Xem gói license
                  </Button>
                </Card>
              </Col>
            );
          })}
        </Row>
      )}

      {selectedProduct && (
        <PlansModal
          product={selectedProduct}
          open={!!selectedProduct}
          onClose={() => setSelectedProduct(null)}
          userBalance={user?.balance ?? 0}
        />
      )}
    </div>
  );
}

function PlansModal({ product, open, onClose, userBalance }: { product: Product; open: boolean; onClose: () => void; userBalance: number }) {
  const queryClient = useQueryClient();
  const { updateUser } = useAuthStore();

  const { data: plansRes, isLoading } = useQuery({
    queryKey: ["plans", product.id],
    queryFn: () => plansApi.getByProduct(product.id, { activeOnly: true }),
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    select: (res) => res.data as any as LicensePlan[],
    enabled: open,
  });

  const purchase = useMutation({
    mutationFn: (planId: string) => licensesApi.purchase(planId),
    onSuccess: async () => {
      message.success("Mua license thành công!");
      queryClient.invalidateQueries({ queryKey: ["my-licenses"] });
      queryClient.invalidateQueries({ queryKey: ["products"] });
      // Refresh user balance from server
      try {
        const res = await apiClient.get("/me");
        const userData = (res.data as any)?.data ?? res.data;
        if (userData?.balance !== undefined) {
          updateUser({ balance: userData.balance });
        }
      } catch {
        // fallback: deduct locally
      }
      onClose();
    },
    onError: (err: any) => {
      message.error(err.response?.data?.message || "Mua license thất bại");
    },
  });

  const plans: LicensePlan[] = plansRes ?? [];

  const getPlanIcon = (index: number) => {
    const icons = [<ThunderboltOutlined key="t" />, <CrownOutlined key="c" />, <SafetyCertificateOutlined key="s" />];
    return icons[index % icons.length];
  };

  return (
    <Modal
      title={
        <Space>
          <SafetyCertificateOutlined style={{ color: "#1677ff" }} />
          <span>Gói License - {product.name}</span>
        </Space>
      }
      open={open}
      onCancel={onClose}
      footer={null}
      width={700}
    >
      <div style={{
        background: "#f6f8fa",
        borderRadius: 8,
        padding: "12px 16px",
        marginBottom: 20,
        display: "flex",
        justifyContent: "space-between",
        alignItems: "center",
      }}>
        <Text>Số dư hiện tại:</Text>
        <Text strong style={{ fontSize: 18, color: "#52c41a" }}>{formatVND(userBalance)}</Text>
      </div>

      {isLoading ? (
        <div style={{ textAlign: "center", padding: 32 }}><Spin size="large" /></div>
      ) : plans.length === 0 ? (
        <Empty description="Chưa có gói license nào" />
      ) : (
        <Row gutter={[16, 16]}>
          {plans.map((plan: LicensePlan, index: number) => {
            const canAfford = userBalance >= plan.price;
            const isPopular = index === Math.min(1, plans.length - 1) && plans.length > 1;
            return (
              <Col xs={24} sm={plans.length <= 2 ? 12 : 8} key={plan.id}>
                <Card
                  style={{
                    height: "100%",
                    border: isPopular ? "2px solid #1677ff" : "1px solid #f0f0f0",
                    position: "relative",
                  }}
                  styles={{ body: { padding: 20, textAlign: "center" } }}
                >
                  {isPopular && (
                    <Tag color="blue" style={{ position: "absolute", top: -1, right: 12 }}>
                      Phổ biến
                    </Tag>
                  )}

                  <div style={{ marginBottom: 12 }}>
                    <div style={{
                      width: 48,
                      height: 48,
                      borderRadius: "50%",
                      background: isPopular ? "#1677ff" : "#f0f0f0",
                      color: isPopular ? "#fff" : "#666",
                      display: "inline-flex",
                      alignItems: "center",
                      justifyContent: "center",
                      fontSize: 20,
                      marginBottom: 8,
                    }}>
                      {getPlanIcon(index)}
                    </div>
                    <Title level={5} style={{ margin: "8px 0 4px" }}>{plan.name}</Title>
                  </div>

                  <div style={{ marginBottom: 16 }}>
                    <Text strong style={{ fontSize: 24, color: "#1677ff" }}>
                      {formatVND(plan.price)}
                    </Text>
                  </div>

                  <Space direction="vertical" size={8} style={{ width: "100%", marginBottom: 16, textAlign: "left" }}>
                    <div>
                      <ClockCircleOutlined style={{ marginRight: 8, color: "#8c8c8c" }} />
                      <Text>{plan.durationDays === 0 ? "Vĩnh viễn" : `${plan.durationDays} ngày`}</Text>
                    </div>
                    <div>
                      <TeamOutlined style={{ marginRight: 8, color: "#8c8c8c" }} />
                      <Text>{plan.maxActivations} thiết bị</Text>
                    </div>
                    {plan.features && (() => {
                      try {
                        const features = JSON.parse(plan.features);
                        return Array.isArray(features) ? features.map((f: string, i: number) => (
                          <div key={i}>
                            <CheckCircleOutlined style={{ marginRight: 8, color: "#52c41a" }} />
                            <Text>{f}</Text>
                          </div>
                        )) : null;
                      } catch { return null; }
                    })()}
                  </Space>

                  <Button
                    type={isPopular ? "primary" : "default"}
                    block
                    size="large"
                    icon={<ShoppingCartOutlined />}
                    disabled={!canAfford}
                    loading={purchase.isPending}
                    onClick={(e) => {
                      e.stopPropagation();
                      Modal.confirm({
                        title: "Xác nhận mua license",
                        content: (
                          <div>
                            <p>Gói: <strong>{plan.name}</strong></p>
                            <p>Giá: <strong>{formatVND(plan.price)}</strong></p>
                            <Divider style={{ margin: "8px 0" }} />
                            <p>Số dư sau khi mua: <strong style={{ color: "#52c41a" }}>{formatVND(userBalance - plan.price)}</strong></p>
                          </div>
                        ),
                        okText: "Mua ngay",
                        cancelText: "Hủy",
                        onOk: () => purchase.mutateAsync(plan.id),
                      });
                    }}
                  >
                    {canAfford ? "Mua ngay" : "Không đủ tiền"}
                  </Button>
                </Card>
              </Col>
            );
          })}
        </Row>
      )}
    </Modal>
  );
}
