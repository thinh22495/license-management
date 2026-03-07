"use client";

import { useState } from "react";
import {
  Card,
  Table,
  Button,
  Modal,
  Form,
  Input,
  Switch,
  Space,
  Typography,
  Tag,
  message,
  Popconfirm,
  InputNumber,
} from "antd";
import { PlusOutlined, EditOutlined, DeleteOutlined, AppstoreOutlined } from "@ant-design/icons";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { productsApi } from "@/lib/api/products.api";
import { plansApi } from "@/lib/api/plans.api";
import { formatVND, formatDate } from "@/lib/utils/format";
import type { Product, LicensePlan } from "@/types";

const { Title, Text } = Typography;
const { TextArea } = Input;

export default function AdminProductsPage() {
  const queryClient = useQueryClient();
  const [productModalOpen, setProductModalOpen] = useState(false);
  const [planModalOpen, setPlanModalOpen] = useState(false);
  const [editingProduct, setEditingProduct] = useState<Product | null>(null);
  const [selectedProductId, setSelectedProductId] = useState<string | null>(null);
  const [productForm] = Form.useForm();
  const [planForm] = Form.useForm();

  const { data: productsRes, isLoading } = useQuery({
    queryKey: ["admin-products"],
    queryFn: () => productsApi.getAll(),
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    select: (res) => (res.data as any)?.items,
  });

  const products = productsRes ?? [];

  const createProduct = useMutation({
    mutationFn: (values: { name: string; slug: string; description: string }) =>
      productsApi.create(values),
    onSuccess: () => {
      message.success("Tạo sản phẩm thành công");
      queryClient.invalidateQueries({ queryKey: ["admin-products"] });
      setProductModalOpen(false);
      productForm.resetFields();
    },
    onError: () => message.error("Lỗi khi tạo sản phẩm"),
  });

  const updateProduct = useMutation({
    mutationFn: ({ id, ...data }: { id: string; name?: string; description?: string; isActive?: boolean }) =>
      productsApi.update(id, data),
    onSuccess: () => {
      message.success("Cập nhật thành công");
      queryClient.invalidateQueries({ queryKey: ["admin-products"] });
      setProductModalOpen(false);
      setEditingProduct(null);
      productForm.resetFields();
    },
    onError: () => message.error("Lỗi khi cập nhật"),
  });

  const deleteProduct = useMutation({
    mutationFn: (id: string) => productsApi.delete(id),
    onSuccess: () => {
      message.success("Xóa thành công");
      queryClient.invalidateQueries({ queryKey: ["admin-products"] });
    },
    onError: () => message.error("Lỗi khi xóa"),
  });

  const createPlan = useMutation({
    mutationFn: ({ productId, ...data }: { productId: string; name: string; durationDays: number; maxActivations: number; price: number; features: string }) =>
      plansApi.create(productId, data),
    onSuccess: () => {
      message.success("Tạo gói license thành công");
      queryClient.invalidateQueries({ queryKey: ["plans"] });
      setPlanModalOpen(false);
      planForm.resetFields();
    },
    onError: () => message.error("Lỗi khi tạo gói"),
  });

  const handleOpenProductModal = (product?: Product) => {
    if (product) {
      setEditingProduct(product);
      productForm.setFieldsValue(product);
    } else {
      setEditingProduct(null);
      productForm.resetFields();
    }
    setProductModalOpen(true);
  };

  const handleProductSubmit = (values: Record<string, string | boolean>) => {
    if (editingProduct) {
      updateProduct.mutate({ id: editingProduct.id, ...values });
    } else {
      createProduct.mutate(values as { name: string; slug: string; description: string });
    }
  };

  const handleOpenPlanModal = (productId: string) => {
    setSelectedProductId(productId);
    planForm.resetFields();
    setPlanModalOpen(true);
  };

  const columns = [
    {
      title: "Tên sản phẩm",
      dataIndex: "name",
      key: "name",
      render: (v: string) => <Text strong>{v}</Text>,
    },
    { title: "Slug", dataIndex: "slug", key: "slug" },
    {
      title: "Trạng thái",
      dataIndex: "isActive",
      key: "isActive",
      render: (v: boolean) => (
        <Tag color={v ? "green" : "red"} style={{ borderRadius: 6, fontWeight: 500 }}>
          {v ? "Hoạt động" : "Ngừng"}
        </Tag>
      ),
    },
    {
      title: "Ngày tạo",
      dataIndex: "createdAt",
      key: "createdAt",
      render: (v: string) => formatDate(v),
    },
    {
      title: "Thao tác",
      key: "actions",
      render: (_: unknown, record: Product) => (
        <Space>
          <Button size="small" icon={<EditOutlined />} onClick={() => handleOpenProductModal(record)} style={{ borderRadius: 8 }}>
            Sửa
          </Button>
          <Button size="small" icon={<PlusOutlined />} onClick={() => handleOpenPlanModal(record.id)} style={{ borderRadius: 8 }}>
            Thêm gói
          </Button>
          <Popconfirm title="Xóa sản phẩm này?" onConfirm={() => deleteProduct.mutate(record.id)}>
            <Button size="small" danger icon={<DeleteOutlined />} style={{ borderRadius: 8 }} />
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <div>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 20 }}>
        <Title level={3} className="page-title" style={{ margin: 0 }}>
          <AppstoreOutlined style={{ marginRight: 10 }} />
          Quản lý sản phẩm
        </Title>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => handleOpenProductModal()} className="btn-gradient" style={{ borderRadius: 10 }}>
          Thêm sản phẩm
        </Button>
      </div>

      <Card className="enhanced-card" styles={{ body: { padding: 0 } }}>
        <Table
          columns={columns}
          dataSource={products}
          rowKey="id"
          loading={isLoading}
          expandable={{
            expandedRowRender: (record: Product) => <PlansTable productId={record.id} />,
          }}
        />
      </Card>

      {/* Product Modal */}
      <Modal
        title={<Text strong style={{ fontSize: 16 }}>{editingProduct ? "Chỉnh sửa sản phẩm" : "Thêm sản phẩm mới"}</Text>}
        open={productModalOpen}
        onCancel={() => { setProductModalOpen(false); setEditingProduct(null); }}
        onOk={() => productForm.submit()}
        confirmLoading={createProduct.isPending || updateProduct.isPending}
      >
        <Form form={productForm} layout="vertical" onFinish={handleProductSubmit}>
          <Form.Item name="name" label="Tên sản phẩm" rules={[{ required: true }]}>
            <Input style={{ borderRadius: 10 }} />
          </Form.Item>
          {!editingProduct && (
            <Form.Item name="slug" label="Slug (URL)" rules={[{ required: true }]}>
              <Input style={{ borderRadius: 10 }} />
            </Form.Item>
          )}
          <Form.Item name="description" label="Mô tả" rules={[{ required: true }]}>
            <TextArea rows={3} style={{ borderRadius: 10 }} />
          </Form.Item>
          <Form.Item name="websiteUrl" label="Website URL">
            <Input style={{ borderRadius: 10 }} />
          </Form.Item>
          <Form.Item name="isActive" label="Hoạt động" valuePropName="checked" initialValue={true}>
            <Switch />
          </Form.Item>
        </Form>
      </Modal>

      {/* Plan Modal */}
      <Modal
        title={<Text strong style={{ fontSize: 16 }}>Thêm gói License</Text>}
        open={planModalOpen}
        onCancel={() => setPlanModalOpen(false)}
        onOk={() => planForm.submit()}
        confirmLoading={createPlan.isPending}
      >
        <Form
          form={planForm}
          layout="vertical"
          onFinish={(values) => createPlan.mutate({ productId: selectedProductId!, ...values })}
        >
          <Form.Item name="name" label="Tên gói" rules={[{ required: true }]}>
            <Input placeholder="VD: Pro - 1 năm" style={{ borderRadius: 10 }} />
          </Form.Item>
          <Form.Item name="durationDays" label="Thời hạn (ngày, 0 = vĩnh viễn)" rules={[{ required: true }]}>
            <InputNumber min={0} style={{ width: "100%", borderRadius: 10 }} />
          </Form.Item>
          <Form.Item name="maxActivations" label="Số thiết bị tối đa" rules={[{ required: true }]}>
            <InputNumber min={1} style={{ width: "100%", borderRadius: 10 }} />
          </Form.Item>
          <Form.Item name="price" label="Giá (VND)" rules={[{ required: true }]}>
            <InputNumber min={0} style={{ width: "100%", borderRadius: 10 }} formatter={(v) => `${v}`.replace(/\B(?=(\d{3})+(?!\d))/g, ",")} />
          </Form.Item>
          <Form.Item name="features" label='Tính năng (JSON array)' rules={[{ required: true }]} initialValue="[]">
            <TextArea rows={2} placeholder='["feature1","feature2"]' style={{ borderRadius: 10 }} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}

function PlansTable({ productId }: { productId: string }) {
  const queryClient = useQueryClient();

  const { data: plansRes, isLoading } = useQuery({
    queryKey: ["plans", productId],
    queryFn: () => plansApi.getByProduct(productId),
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    select: (res) => res.data as any as LicensePlan[],
  });

  const deletePlan = useMutation({
    mutationFn: (id: string) => plansApi.delete(id),
    onSuccess: () => {
      message.success("Xóa gói thành công");
      queryClient.invalidateQueries({ queryKey: ["plans", productId] });
    },
  });

  const plans = plansRes ?? [];

  const planColumns = [
    { title: "Tên gói", dataIndex: "name", key: "name", render: (v: string) => <Text strong>{v}</Text> },
    {
      title: "Thời hạn",
      dataIndex: "durationDays",
      key: "durationDays",
      render: (v: number) => v === 0 ? <Tag color="blue" style={{ borderRadius: 6 }}>Vĩnh viễn</Tag> : `${v} ngày`,
    },
    { title: "Thiết bị tối đa", dataIndex: "maxActivations", key: "maxActivations" },
    {
      title: "Giá",
      dataIndex: "price",
      key: "price",
      render: (v: number) => <Text strong style={{ color: "#4f46e5" }}>{formatVND(v)}</Text>,
    },
    {
      title: "Trạng thái",
      dataIndex: "isActive",
      key: "isActive",
      render: (v: boolean) => <Tag color={v ? "green" : "red"} style={{ borderRadius: 6, fontWeight: 500 }}>{v ? "Bán" : "Ngừng"}</Tag>,
    },
    {
      title: "",
      key: "actions",
      render: (_: unknown, record: LicensePlan) => (
        <Popconfirm title="Xóa gói này?" onConfirm={() => deletePlan.mutate(record.id)}>
          <Button size="small" danger icon={<DeleteOutlined />} style={{ borderRadius: 8 }} />
        </Popconfirm>
      ),
    },
  ];

  return (
    <Table
      columns={planColumns}
      dataSource={plans}
      rowKey="id"
      loading={isLoading}
      pagination={false}
      size="small"
    />
  );
}
