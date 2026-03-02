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
  List,
  Space,
  message,
  Spin,
} from "antd";
import {
  ShoppingCartOutlined,
  CheckCircleOutlined,
  ClockCircleOutlined,
  TeamOutlined,
} from "@ant-design/icons";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { productsApi } from "@/lib/api/products.api";
import { plansApi } from "@/lib/api/plans.api";
import { licensesApi } from "@/lib/api/licenses.api";
import { formatVND } from "@/lib/utils/format";
import { useAuthStore } from "@/lib/stores/auth.store";
import type { Product, LicensePlan } from "@/types";

const { Title, Text, Paragraph } = Typography;

export default function ProductsPage() {
  const queryClient = useQueryClient();
  const { user } = useAuthStore();
  const [selectedProduct, setSelectedProduct] = useState<Product | null>(null);

  const { data: productsRes, isLoading } = useQuery({
    queryKey: ["products"],
    queryFn: () => productsApi.getAll({ activeOnly: true }),
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    select: (res) => (res.data as any)?.items,
  });

  const products = productsRes ?? [];

  return (
    <div>
      <Title level={3}>Sản phẩm</Title>
      <Paragraph type="secondary">Chọn sản phẩm để xem các gói license có sẵn</Paragraph>

      {isLoading ? (
        <div style={{ textAlign: "center", padding: 48 }}><Spin size="large" /></div>
      ) : (
        <Row gutter={[16, 16]}>
          {products.map((product: Product) => (
            <Col xs={24} sm={12} lg={8} key={product.id}>
              <Card
                hoverable
                onClick={() => setSelectedProduct(product)}
                actions={[
                  <Button type="link" key="view" icon={<ShoppingCartOutlined />}>
                    Xem gói license
                  </Button>,
                ]}
              >
                <Card.Meta
                  title={product.name}
                  description={product.description}
                />
              </Card>
            </Col>
          ))}
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

  const { data: plansRes, isLoading } = useQuery({
    queryKey: ["plans", product.id],
    queryFn: () => plansApi.getByProduct(product.id, { activeOnly: true }),
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    select: (res) => res.data as any as LicensePlan[],
    enabled: open,
  });

  const purchase = useMutation({
    mutationFn: (planId: string) => licensesApi.purchase(planId),
    onSuccess: () => {
      message.success("Mua license thành công!");
      queryClient.invalidateQueries({ queryKey: ["my-licenses"] });
      onClose();
    },
    onError: (err: any) => {
      message.error(err.response?.data?.message || "Mua license thất bại");
    },
  });

  const plans = plansRes ?? [];

  return (
    <Modal
      title={`Gói License - ${product.name}`}
      open={open}
      onCancel={onClose}
      footer={null}
      width={600}
    >
      <Paragraph type="secondary" style={{ marginBottom: 16 }}>
        Số dư hiện tại: <Text strong>{formatVND(userBalance)}</Text>
      </Paragraph>

      {isLoading ? (
        <Spin />
      ) : (
        <List
          dataSource={plans}
          renderItem={(plan: LicensePlan) => {
            const canAfford = userBalance >= plan.price;
            return (
              <Card style={{ marginBottom: 12 }} size="small">
                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                  <div>
                    <Text strong style={{ fontSize: 16 }}>{plan.name}</Text>
                    <div style={{ marginTop: 4 }}>
                      <Space>
                        <Tag icon={<ClockCircleOutlined />}>
                          {plan.durationDays === 0 ? "Vĩnh viễn" : `${plan.durationDays} ngày`}
                        </Tag>
                        <Tag icon={<TeamOutlined />}>{plan.maxActivations} thiết bị</Tag>
                      </Space>
                    </div>
                    <Text strong style={{ fontSize: 18, color: "#1677ff", display: "block", marginTop: 8 }}>
                      {formatVND(plan.price)}
                    </Text>
                  </div>
                  <Button
                    type="primary"
                    icon={<ShoppingCartOutlined />}
                    disabled={!canAfford}
                    loading={purchase.isPending}
                    onClick={() => {
                      Modal.confirm({
                        title: "Xác nhận mua license",
                        content: `Bạn sẽ mua gói "${plan.name}" với giá ${formatVND(plan.price)}. Số dư sẽ còn ${formatVND(userBalance - plan.price)}.`,
                        okText: "Mua ngay",
                        cancelText: "Hủy",
                        onOk: () => purchase.mutateAsync(plan.id),
                      });
                    }}
                  >
                    {canAfford ? "Mua ngay" : "Không đủ tiền"}
                  </Button>
                </div>
              </Card>
            );
          }}
        />
      )}
    </Modal>
  );
}
