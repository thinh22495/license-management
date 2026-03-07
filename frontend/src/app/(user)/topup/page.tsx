"use client";

import { useState } from "react";
import {
  Card,
  Typography,
  Row,
  Col,
  Button,
  InputNumber,
  Radio,
  Space,
  message,
  Divider,
  Alert,
} from "antd";
import {
  WalletOutlined,
  CreditCardOutlined,
  CheckCircleOutlined,
} from "@ant-design/icons";
import { useMutation } from "@tanstack/react-query";
import { paymentsApi } from "@/lib/api/payments.api";
import { useAuthStore } from "@/lib/stores/auth.store";
import { formatVND } from "@/lib/utils/format";

const { Title, Text } = Typography;

const presetAmounts = [50_000, 100_000, 200_000, 500_000, 1_000_000, 2_000_000];

const paymentMethods = [
  { value: "momo", label: "MoMo", color: "#a50064" },
  { value: "vnpay", label: "VNPay", color: "#0066b3" },
  { value: "zalopay", label: "ZaloPay", color: "#008fe5" },
];

export default function TopUpPage() {
  const { user } = useAuthStore();
  const [amount, setAmount] = useState<number>(100_000);
  const [paymentMethod, setPaymentMethod] = useState<string>("momo");

  const topUp = useMutation({
    mutationFn: () =>
      paymentsApi.topUp({
        amount,
        paymentMethod,
        returnUrl: `${window.location.origin}/topup/result`,
      }),
    onSuccess: (res) => {
      const data = res.data.data;
      if (data?.paymentUrl) {
        message.loading("Đang chuyển đến cổng thanh toán...");
        window.location.href = data.paymentUrl;
      } else {
        message.error("Không nhận được URL thanh toán");
      }
    },
    onError: (err: any) => {
      message.error(err.response?.data?.message || "Tạo đơn nạp tiền thất bại");
    },
  });

  return (
    <div>
      <Title level={3} className="page-title">
        <WalletOutlined style={{ marginRight: 10 }} />
        Nạp tiền
      </Title>

      <Row gutter={[24, 24]}>
        <Col xs={24} md={16}>
          <Card className="enhanced-card" title={
            <Text strong style={{ fontSize: 16 }}>
              <CreditCardOutlined style={{ marginRight: 8, color: "#4f46e5" }} />
              Chọn số tiền nạp
            </Text>
          }>
            <Row gutter={[12, 12]} style={{ marginBottom: 20 }}>
              {presetAmounts.map((a) => (
                <Col key={a} xs={8} sm={6} md={4}>
                  <Button
                    block
                    type={amount === a ? "primary" : "default"}
                    onClick={() => setAmount(a)}
                    className={amount === a ? "btn-gradient" : ""}
                    style={{
                      borderRadius: 10,
                      height: 42,
                      fontWeight: amount === a ? 600 : 400,
                    }}
                  >
                    {formatVND(a)}
                  </Button>
                </Col>
              ))}
            </Row>

            <div style={{ marginBottom: 28 }}>
              <Text type="secondary" style={{ marginBottom: 8, display: "block" }}>Hoặc nhập số tiền khác:</Text>
              <Space.Compact style={{ width: "100%" }}>
                <InputNumber
                  style={{ width: "100%", borderRadius: "10px 0 0 10px" }}
                  min={10_000}
                  max={50_000_000}
                  step={10_000}
                  value={amount}
                  onChange={(v) => v && setAmount(v)}
                  formatter={(v) => `${v}`.replace(/\B(?=(\d{3})+(?!\d))/g, ",")}
                  size="large"
                />
                <Button size="large" disabled style={{ borderRadius: "0 10px 10px 0" }}>VND</Button>
              </Space.Compact>
            </div>

            <Divider />

            <div style={{ marginBottom: 28 }}>
              <Text strong style={{ display: "block", marginBottom: 14, fontSize: 15 }}>
                Phương thức thanh toán:
              </Text>
              <Radio.Group value={paymentMethod} onChange={(e) => setPaymentMethod(e.target.value)} style={{ width: "100%" }}>
                <Row gutter={[12, 12]}>
                  {paymentMethods.map((pm) => (
                    <Col xs={8} key={pm.value}>
                      <Radio.Button
                        value={pm.value}
                        style={{
                          width: "100%",
                          height: 56,
                          display: "flex",
                          alignItems: "center",
                          justifyContent: "center",
                          borderRadius: 12,
                          fontWeight: 600,
                          fontSize: 15,
                          border: paymentMethod === pm.value ? `2px solid ${pm.color}` : "1px solid #e5e7eb",
                          color: paymentMethod === pm.value ? pm.color : "#6b7280",
                          background: paymentMethod === pm.value ? `${pm.color}08` : "#fff",
                        }}
                      >
                        {pm.label}
                      </Radio.Button>
                    </Col>
                  ))}
                </Row>
              </Radio.Group>
            </div>

            <Button
              type="primary"
              size="large"
              block
              icon={<WalletOutlined />}
              loading={topUp.isPending}
              onClick={() => topUp.mutate()}
              className="btn-gradient"
              style={{ height: 50, borderRadius: 12, fontSize: 16, fontWeight: 600 }}
            >
              Nạp {formatVND(amount)}
            </Button>
          </Card>
        </Col>

        <Col xs={24} md={8}>
          <Card className="enhanced-card" styles={{ body: { padding: 0, overflow: "hidden" } }}>
            <div style={{
              background: "linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%)",
              padding: "28px 24px",
              color: "#fff",
            }}>
              <Text style={{ color: "rgba(255,255,255,0.8)", fontSize: 13 }}>Số dư hiện tại</Text>
              <Title level={2} style={{ margin: "4px 0 0", color: "#fff", fontWeight: 800 }}>
                {formatVND(user?.balance ?? 0)}
              </Title>
            </div>
            <div style={{ padding: "20px 24px" }}>
              <div style={{
                background: "#f0fdf4",
                borderRadius: 10,
                padding: "14px 16px",
                border: "1px solid #bbf7d0",
              }}>
                <div style={{ display: "flex", alignItems: "center", gap: 8, marginBottom: 4 }}>
                  <CheckCircleOutlined style={{ color: "#10b981" }} />
                  <Text type="secondary" style={{ fontSize: 13 }}>Sau khi nạp</Text>
                </div>
                <Title level={3} style={{ margin: 0, color: "#10b981", fontWeight: 700 }}>
                  {formatVND((user?.balance ?? 0) + amount)}
                </Title>
              </div>
            </div>
          </Card>

          <Alert
            type="info"
            showIcon
            style={{ marginTop: 16, borderRadius: 12, border: "1px solid #c7d2fe" }}
            message={<Text strong>Lưu ý</Text>}
            description="Tiền nạp sẽ được cộng vào tài khoản ngay sau khi thanh toán thành công. Mọi giao dịch đều được ghi nhận trong lịch sử."
          />
        </Col>
      </Row>
    </div>
  );
}
