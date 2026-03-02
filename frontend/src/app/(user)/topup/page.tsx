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
import { WalletOutlined } from "@ant-design/icons";
import { useMutation } from "@tanstack/react-query";
import { paymentsApi } from "@/lib/api/payments.api";
import { useAuthStore } from "@/lib/stores/auth.store";
import { formatVND } from "@/lib/utils/format";

const { Title, Text } = Typography;

const presetAmounts = [50_000, 100_000, 200_000, 500_000, 1_000_000, 2_000_000];

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
      <Title level={3}>Nạp tiền</Title>

      <Row gutter={[24, 24]}>
        <Col xs={24} md={16}>
          <Card title="Chọn số tiền nạp">
            <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
              {presetAmounts.map((a) => (
                <Col key={a} xs={8} sm={6} md={4}>
                  <Button
                    block
                    type={amount === a ? "primary" : "default"}
                    onClick={() => setAmount(a)}
                  >
                    {formatVND(a)}
                  </Button>
                </Col>
              ))}
            </Row>

            <div style={{ marginBottom: 24 }}>
              <Text>Hoặc nhập số tiền khác:</Text>
              <InputNumber
                style={{ width: "100%", marginTop: 8 }}
                min={10_000}
                max={50_000_000}
                step={10_000}
                value={amount}
                onChange={(v) => v && setAmount(v)}
                formatter={(v) => `${v}`.replace(/\B(?=(\d{3})+(?!\d))/g, ",")}
                addonAfter="VND"
                size="large"
              />
            </div>

            <Divider />

            <div style={{ marginBottom: 24 }}>
              <Text strong style={{ display: "block", marginBottom: 12 }}>Phương thức thanh toán:</Text>
              <Radio.Group value={paymentMethod} onChange={(e) => setPaymentMethod(e.target.value)}>
                <Space orientation="vertical">
                  <Radio value="momo">MoMo</Radio>
                  <Radio value="vnpay">VNPay</Radio>
                  <Radio value="zalopay">ZaloPay</Radio>
                </Space>
              </Radio.Group>
            </div>

            <Button
              type="primary"
              size="large"
              block
              icon={<WalletOutlined />}
              loading={topUp.isPending}
              onClick={() => topUp.mutate()}
            >
              Nạp {formatVND(amount)}
            </Button>
          </Card>
        </Col>

        <Col xs={24} md={8}>
          <Card title="Thông tin tài khoản">
            <div style={{ marginBottom: 16 }}>
              <Text type="secondary">Số dư hiện tại</Text>
              <Title level={3} style={{ margin: "4px 0 0" }}>
                {formatVND(user?.balance ?? 0)}
              </Title>
            </div>
            <div>
              <Text type="secondary">Sau khi nạp</Text>
              <Title level={4} style={{ margin: "4px 0 0", color: "#52c41a" }}>
                {formatVND((user?.balance ?? 0) + amount)}
              </Title>
            </div>
          </Card>

          <Alert
            type="info"
            showIcon
            style={{ marginTop: 16 }}
            message="Lưu ý"
            description="Tiền nạp sẽ được cộng vào tài khoản ngay sau khi thanh toán thành công. Mọi giao dịch đều được ghi nhận trong lịch sử."
          />
        </Col>
      </Row>
    </div>
  );
}
