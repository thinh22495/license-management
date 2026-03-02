"use client";

import { useSearchParams } from "next/navigation";
import { Button, Result } from "antd";
import Link from "next/link";
import { Suspense } from "react";

function TopUpResultContent() {
  const searchParams = useSearchParams();
  const status = searchParams.get("status");
  const msg = searchParams.get("message");

  if (status === "success") {
    return (
      <Result
        status="success"
        title="Nạp tiền thành công!"
        subTitle={msg || "Số dư đã được cập nhật vào tài khoản của bạn."}
        extra={[
          <Link key="dashboard" href="/dashboard">
            <Button type="primary">Về Dashboard</Button>
          </Link>,
          <Link key="products" href="/products">
            <Button>Mua License</Button>
          </Link>,
        ]}
      />
    );
  }

  return (
    <Result
      status="error"
      title="Nạp tiền thất bại"
      subTitle={msg || "Đã có lỗi xảy ra trong quá trình thanh toán. Vui lòng thử lại."}
      extra={[
        <Link key="retry" href="/topup">
          <Button type="primary">Thử lại</Button>
        </Link>,
        <Link key="history" href="/transactions">
          <Button>Xem lịch sử</Button>
        </Link>,
      ]}
    />
  );
}

export default function TopUpResultPage() {
  return (
    <Suspense>
      <TopUpResultContent />
    </Suspense>
  );
}
