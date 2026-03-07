import type { Metadata } from "next";
import { AntdRegistry } from "@ant-design/nextjs-registry";
import { App } from "antd";
import "./globals.css";
import { Providers } from "./providers";

export const metadata: Metadata = {
  title: "License Management",
  description: "Software License Management System",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="vi">
      <body>
        <AntdRegistry>
          <App>
            <Providers>{children}</Providers>
          </App>
        </AntdRegistry>
      </body>
    </html>
  );
}
