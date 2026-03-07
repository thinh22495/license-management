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

const { Title, Text } = Typography;

const statCardConfigs = [
  { key: "totalUsers", title: "Tổng người dùng", icon: <UserOutlined style={{ fontSize: 24, color: "#fff" }} />, className: "stat-card stat-card-blue" },
  { key: "activeLicenses", title: "License hoạt động", icon: <KeyOutlined style={{ fontSize: 24, color: "#fff" }} />, className: "stat-card stat-card-green" },
  { key: "totalProducts", title: "Sản phẩm", icon: <AppstoreOutlined style={{ fontSize: 24, color: "#fff" }} />, className: "stat-card stat-card-purple" },
  { key: "revenueThisMonth", title: "Doanh thu tháng", icon: <DollarOutlined style={{ fontSize: 24, color: "#fff" }} />, className: "stat-card stat-card-orange", isVND: true },
  { key: "newUsersThisMonth", title: "User mới tháng", icon: <RiseOutlined style={{ fontSize: 24, color: "#fff" }} />, className: "stat-card stat-card-blue" },
  { key: "licensesPurchasedThisMonth", title: "License bán tháng", icon: <ShoppingCartOutlined style={{ fontSize: 24, color: "#fff" }} />, className: "stat-card stat-card-green" },
];

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
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 24 }}>
        <Title level={3} className="page-title" style={{ margin: 0 }}>Admin Dashboard</Title>
        <Select
          value={days}
          onChange={setDays}
          style={{ width: 160 }}
          options={[
            { value: 7, label: "7 ngày qua" },
            { value: 14, label: "14 ngày qua" },
            { value: 30, label: "30 ngày qua" },
            { value: 90, label: "90 ngày qua" },
          ]}
        />
      </div>

      {/* Stats Cards */}
      <Row gutter={[16, 16]} style={{ marginBottom: 28 }}>
        {statCardConfigs.map((config, index) => (
          <Col xs={24} sm={12} lg={4} key={config.key}>
            <Card
              loading={statsLoading}
              className={`${config.className} stagger-item animate-fade-in-up`}
              styles={{ body: { padding: 20, position: "relative", zIndex: 1 } }}
            >
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start" }}>
                <div>
                  <Text style={{ color: "rgba(255,255,255,0.8)", fontSize: 12 }}>{config.title}</Text>
                  <div style={{ fontSize: 24, fontWeight: 700, color: "#fff", marginTop: 4 }}>
                    {config.isVND
                      ? formatVND(Number((stats as any)?.[config.key] ?? 0))
                      : (stats as any)?.[config.key] ?? 0
                    }
                  </div>
                </div>
                <div style={{
                  width: 44,
                  height: 44,
                  borderRadius: 12,
                  background: "rgba(255,255,255,0.15)",
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "center",
                }}>
                  {config.icon}
                </div>
              </div>
            </Card>
          </Col>
        ))}
      </Row>

      {/* Charts */}
      {chartsLoading ? (
        <div style={{ textAlign: "center", padding: 48 }}><Spin size="large" /></div>
      ) : (
        <>
          {/* Revenue Chart */}
          <Row gutter={[20, 20]} style={{ marginBottom: 20 }}>
            <Col xs={24} lg={16}>
              <Card title={<Text strong style={{ fontSize: 16 }}>Doanh thu</Text>} className="chart-card">
                {charts?.revenue && (
                  <Line
                    data={charts.revenue}
                    xField="date"
                    yField="amount"
                    height={300}
                    shapeField="smooth"
                    style={{ lineWidth: 3, stroke: "#4f46e5" }}
                    axis={{ y: { labelFormatter: (v: number) => formatVND(v) } }}
                    tooltip={{ items: [{ channel: "y", name: "Doanh thu", valueFormatter: (v: number) => formatVND(v) }] }}
                  />
                )}
              </Card>
            </Col>
            <Col xs={24} lg={8}>
              <Card title={<Text strong style={{ fontSize: 16 }}>Doanh thu theo sản phẩm</Text>} className="chart-card">
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
                  <div style={{ height: 300, display: "flex", alignItems: "center", justifyContent: "center", color: "#9ca3af" }}>
                    Chưa có dữ liệu
                  </div>
                )}
              </Card>
            </Col>
          </Row>

          {/* Licenses & Users Charts */}
          <Row gutter={[20, 20]}>
            <Col xs={24} lg={12}>
              <Card title={<Text strong style={{ fontSize: 16 }}>License bán ra</Text>} className="chart-card">
                {charts?.licenses && (
                  <Column
                    data={charts.licenses}
                    xField="date"
                    yField="count"
                    height={250}
                    style={{ fill: "#4f46e5", radiusTopLeft: 6, radiusTopRight: 6 }}
                    tooltip={{ items: [{ channel: "y", name: "Licenses" }] }}
                  />
                )}
              </Card>
            </Col>
            <Col xs={24} lg={12}>
              <Card title={<Text strong style={{ fontSize: 16 }}>Người dùng đăng ký</Text>} className="chart-card">
                {charts?.users && (
                  <Column
                    data={charts.users}
                    xField="date"
                    yField="count"
                    height={250}
                    style={{ fill: "#10b981", radiusTopLeft: 6, radiusTopRight: 6 }}
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
