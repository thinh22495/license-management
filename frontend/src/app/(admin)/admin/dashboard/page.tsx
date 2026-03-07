"use client";

import { Card, Row, Col, Statistic, Typography, Spin, Select } from "antd";
import {
  UserOutlined,
  KeyOutlined,
  AppstoreOutlined,
  DollarOutlined,
  RiseOutlined,
  ShoppingCartOutlined,
} from "@ant-design/icons";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { dashboardApi } from "@/lib/api/dashboard.api";
import { formatVND } from "@/lib/utils/format";
import dynamic from "next/dynamic";

const Line = dynamic(() => import("@ant-design/charts").then((m) => m.Line), { ssr: false });
const Column = dynamic(() => import("@ant-design/charts").then((m) => m.Column), { ssr: false });
const Pie = dynamic(() => import("@ant-design/charts").then((m) => m.Pie), { ssr: false });

const { Title } = Typography;

export default function AdminDashboardPage() {
  const [days, setDays] = useState(30);

  const { data: stats, isLoading: statsLoading } = useQuery({
    queryKey: ["dashboard-stats"],
    queryFn: () => dashboardApi.getStats(),
    select: (res) => res.data.data,
  });

  const { data: charts, isLoading: chartsLoading } = useQuery({
    queryKey: ["dashboard-charts", days],
    queryFn: () => dashboardApi.getCharts(days),
    select: (res) => res.data.data,
  });

  return (
    <div>
      <div style={{ display: "flex", justifyContent: "space-between", marginBottom: 16 }}>
        <Title level={3} style={{ margin: 0 }}>Admin Dashboard</Title>
        <Select
          value={days}
          onChange={setDays}
          style={{ width: 150 }}
          options={[
            { value: 7, label: "7 ngày" },
            { value: 14, label: "14 ngày" },
            { value: 30, label: "30 ngày" },
            { value: 90, label: "90 ngày" },
          ]}
        />
      </div>

      {/* Stats Cards */}
      <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
        <Col xs={24} sm={12} lg={4}>
          <Card loading={statsLoading}>
            <Statistic
              title="Tổng người dùng"
              value={stats?.totalUsers ?? 0}
              prefix={<UserOutlined />}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={4}>
          <Card loading={statsLoading}>
            <Statistic
              title="License hoạt động"
              value={stats?.activeLicenses ?? 0}
              prefix={<KeyOutlined />}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={4}>
          <Card loading={statsLoading}>
            <Statistic
              title="Sản phẩm"
              value={stats?.totalProducts ?? 0}
              prefix={<AppstoreOutlined />}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={4}>
          <Card loading={statsLoading}>
            <Statistic
              title="Doanh thu tháng"
              value={stats?.revenueThisMonth ?? 0}
              prefix={<DollarOutlined />}
              formatter={(v) => formatVND(Number(v))}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={4}>
          <Card loading={statsLoading}>
            <Statistic
              title="User mới tháng"
              value={stats?.newUsersThisMonth ?? 0}
              prefix={<RiseOutlined />}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={4}>
          <Card loading={statsLoading}>
            <Statistic
              title="License bán tháng"
              value={stats?.licensesPurchasedThisMonth ?? 0}
              prefix={<ShoppingCartOutlined />}
            />
          </Card>
        </Col>
      </Row>

      {/* Charts */}
      {chartsLoading ? (
        <div style={{ textAlign: "center", padding: 48 }}><Spin size="large" /></div>
      ) : (
        <>
          {/* Revenue Chart */}
          <Row gutter={[16, 16]} style={{ marginBottom: 16 }}>
            <Col xs={24} lg={16}>
              <Card title="Doanh thu">
                {charts?.revenue && (
                  <Line
                    data={charts.revenue}
                    xField="date"
                    yField="amount"
                    height={300}
                    shapeField="smooth"
                    style={{ lineWidth: 2 }}
                    axis={{ y: { labelFormatter: (v: number) => formatVND(v) } }}
                    tooltip={{ items: [{ channel: "y", name: "Doanh thu", valueFormatter: (v: number) => formatVND(v) }] }}
                  />
                )}
              </Card>
            </Col>
            <Col xs={24} lg={8}>
              <Card title="Doanh thu theo sản phẩm">
                {charts?.productRevenue && charts.productRevenue.length > 0 ? (
                  <Pie
                    data={charts.productRevenue}
                    angleField="revenue"
                    colorField="productName"
                    height={300}
                    radius={0.8}
                    innerRadius={0.6}
                    label={false}
                    tooltip={{ items: [{ channel: "y", name: "Doanh thu", valueFormatter: (v: number) => formatVND(v) }] }}
                  />
                ) : (
                  <div style={{ height: 300, display: "flex", alignItems: "center", justifyContent: "center", color: "#999" }}>
                    Chưa có dữ liệu
                  </div>
                )}
              </Card>
            </Col>
          </Row>

          {/* Licenses & Users Charts */}
          <Row gutter={[16, 16]}>
            <Col xs={24} lg={12}>
              <Card title="License bán ra">
                {charts?.licenses && (
                  <Column
                    data={charts.licenses}
                    xField="date"
                    yField="count"
                    height={250}
                    style={{ fill: "#1677ff" }}
                    tooltip={{ items: [{ channel: "y", name: "Licenses" }] }}
                  />
                )}
              </Card>
            </Col>
            <Col xs={24} lg={12}>
              <Card title="Người dùng đăng ký">
                {charts?.users && (
                  <Column
                    data={charts.users}
                    xField="date"
                    yField="count"
                    height={250}
                    style={{ fill: "#52c41a" }}
                    tooltip={{ items: [{ channel: "y", name: "Users" }] }}
                  />
                )}
              </Card>
            </Col>
          </Row>
        </>
      )}
    </div>
  );
}
