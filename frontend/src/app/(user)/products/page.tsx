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
  App,
  Spin,
  Empty,
  Divider,
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

const productGradients = [
  "linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%)",
  "linear-gradient(135deg, #06b6d4 0%, #0891b2 100%)",
  "linear-gradient(135deg, #8b5cf6 0%, #a855f7 100%)",
  "linear-gradient(135deg, #ec4899 0%, #f43f5e 100%)",
  "linear-gradient(135deg, #f59e0b 0%, #ef4444 100%)",
  "linear-gradient(135deg, #10b981 0%, #059669 100%)",
];

const productColors = ["#4f46e5", "#06b6d4", "#8b5cf6", "#ec4899", "#f59e0b", "#10b981"];

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
      <div style={{ marginBottom: 28 }}>
        <Title level={3} className="page-title">
          <ShoppingCartOutlined style={{ marginRight: 10 }} />
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
            const gradient = productGradients[index % productGradients.length];
            const color = productColors[index % productColors.length];
            return (
              <Col xs={24} sm={12} lg={8} key={product.id}>
                <Card
                  hoverable
                  onClick={() => setSelectedProduct(product)}
                  className="product-card stagger-item animate-fade-in-up"
                  style={{ height: "100%" }}
                  styles={{ body: { padding: 0, display: "flex", flexDirection: "column", height: "100%" } }}
                >
                  {/* Gradient header */}
                  <div style={{
                    background: gradient,
                    padding: "28px 24px 24px",
                    position: "relative",
                    overflow: "hidden",
                  }}>
                    <div style={{
                      position: "absolute",
                      top: -20,
                      right: -20,
                      width: 100,
                      height: 100,
                      borderRadius: "50%",
                      background: "rgba(255,255,255,0.1)",
                    }} />
                    <div style={{
                      width: 56,
                      height: 56,
                      borderRadius: 14,
                      background: "rgba(255,255,255,0.2)",
                      display: "flex",
                      alignItems: "center",
                      justifyContent: "center",
                      marginBottom: 12,
                    }}>
                      <SafetyCertificateOutlined style={{ fontSize: 28, color: "#fff" }} />
                    </div>
                    <Title level={4} style={{ margin: 0, color: "#fff" }}>{product.name}</Title>
                  </div>

                  {/* Content */}
                  <div style={{ padding: 24, flex: 1, display: "flex", flexDirection: "column" }}>
                    <Paragraph
                      type="secondary"
                      ellipsis={{ rows: 2 }}
                      style={{ flex: 1, marginBottom: 16 }}
                    >
                      {product.description || "Phần mềm chuyên nghiệp"}
                    </Paragraph>

                    {product.websiteUrl && (
                      <div style={{ marginBottom: 16 }}>
                        <Tag icon={<GlobalOutlined />} color="blue" style={{ borderRadius: 6 }}>
                          {new URL(product.websiteUrl).hostname}
                        </Tag>
                      </div>
                    )}

                    <Button
                      type="primary"
                      block
                      size="large"
                      icon={<ShoppingCartOutlined />}
                      style={{
                        background: color,
                        borderColor: color,
                        borderRadius: 10,
                        height: 44,
                        fontWeight: 600,
                      }}
                    >
                      Xem gói license
                    </Button>
                  </div>
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
  const { message } = App.useApp();
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
      try {
        const res = await apiClient.get("/me");
        const userData = (res.data as any)?.data ?? res.data;
        if (userData?.balance !== undefined) {
          updateUser({ balance: userData.balance });
        }
      } catch {
        // fallback
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
          <SafetyCertificateOutlined style={{ color: "#4f46e5" }} />
          <span style={{ fontWeight: 700 }}>Gói License - {product.name}</span>
        </Space>
      }
      open={open}
      onCancel={onClose}
      footer={null}
      width={720}
      styles={{ body: { padding: "20px 24px" } }}
    >
      <div style={{
        background: "linear-gradient(135deg, #f5f3ff 0%, #eef2ff 100%)",
        borderRadius: 12,
        padding: "14px 20px",
        marginBottom: 24,
        display: "flex",
        justifyContent: "space-between",
        alignItems: "center",
        border: "1px solid #e0e7ff",
      }}>
        <Text style={{ color: "#6b7280" }}>Số dư hiện tại:</Text>
        <Text strong style={{ fontSize: 20, color: "#10b981" }}>{formatVND(userBalance)}</Text>
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
                    border: isPopular ? "2px solid #4f46e5" : "1px solid #e5e7eb",
                    borderRadius: 16,
                    position: "relative",
                    overflow: "hidden",
                  }}
                  styles={{ body: { padding: 24, textAlign: "center", display: "flex", flexDirection: "column", height: "100%" } }}
                >
                  {isPopular && (
                    <div style={{
                      position: "absolute",
                      top: 0,
                      left: 0,
                      right: 0,
                      height: 4,
                      background: "linear-gradient(90deg, #4f46e5, #7c3aed)",
                    }} />
                  )}
                  {isPopular && (
                    <Tag color="purple" style={{
                      position: "absolute",
                      top: 10,
                      right: 10,
                      borderRadius: 6,
                      fontWeight: 600,
                    }}>
                      Phổ biến
                    </Tag>
                  )}

                  <div style={{ marginBottom: 16 }}>
                    <div style={{
                      width: 52,
                      height: 52,
                      borderRadius: "50%",
                      background: isPopular ? "linear-gradient(135deg, #4f46e5, #7c3aed)" : "#f3f4f6",
                      color: isPopular ? "#fff" : "#6b7280",
                      display: "inline-flex",
                      alignItems: "center",
                      justifyContent: "center",
                      fontSize: 22,
                      marginBottom: 12,
                    }}>
                      {getPlanIcon(index)}
                    </div>
                    <Title level={5} style={{ margin: "0 0 4px", fontWeight: 700 }}>{plan.name}</Title>
                  </div>

                  <div style={{ marginBottom: 20 }}>
                    <Text strong style={{ fontSize: 28, color: "#4f46e5", fontWeight: 800 }}>
                      {formatVND(plan.price)}
                    </Text>
                  </div>

                  <Space orientation="vertical" size={10} style={{ width: "100%", marginBottom: 20, textAlign: "left", flex: 1 }}>
                    <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
                      <ClockCircleOutlined style={{ color: "#9ca3af" }} />
                      <Text>{plan.durationDays === 0 ? "Vĩnh viễn" : `${plan.durationDays} ngày`}</Text>
                    </div>
                    <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
                      <TeamOutlined style={{ color: "#9ca3af" }} />
                      <Text>{plan.maxActivations} thiết bị</Text>
                    </div>
                    {plan.features && (() => {
                      try {
                        const features = JSON.parse(plan.features);
                        return Array.isArray(features) ? features.map((f: string, i: number) => (
                          <div key={i} style={{ display: "flex", alignItems: "center", gap: 10 }}>
                            <CheckCircleOutlined style={{ color: "#10b981" }} />
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
                    className={isPopular ? "btn-gradient" : ""}
                    style={{ borderRadius: 10, height: 44, fontWeight: 600, marginTop: "auto" }}
                    onClick={(e) => {
                      e.stopPropagation();
                      Modal.confirm({
                        title: "Xác nhận mua license",
                        content: (
                          <div>
                            <p>Gói: <strong>{plan.name}</strong></p>
                            <p>Giá: <strong>{formatVND(plan.price)}</strong></p>
                            <Divider style={{ margin: "8px 0" }} />
                            <p>Số dư sau khi mua: <strong style={{ color: "#10b981" }}>{formatVND(userBalance - plan.price)}</strong></p>
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
