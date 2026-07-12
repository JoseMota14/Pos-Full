export type OrderItemStatus = "Ordered" | "BeingPrepared" | "Ready" | "Delivered" | "Cancelled";
export type TableAccountStatus = "Open" | "Closed" | "Paid" | "Cancelled";

export interface Category {
  id: string;
  name: string;
  sortOrder: number;
  isActive: boolean;
}

export interface Product {
  id: string;
  name: string;
  description?: string;
  price: number;
  categoryId: string;
  categoryName?: string;
  photoUrl?: string;
  photoAltText?: string;
  isActive: boolean;
}

export interface Table {
  id: string;
  name: string;
  seats: number;
  isActive: boolean;
}

export interface DraftItem {
  product: Product;
  quantity: number;
  notes: string;
}

export interface OrderItem {
  id: string;
  orderId: string;
  productId: string;
  productName: string;
  categoryName?: string;
  quantity: number;
  unitPrice: number;
  notes?: string;
  status: OrderItemStatus;
  createdAt: string;
}

export interface Order {
  id: string;
  tableAccountId: string;
  tableId: string;
  tableName: string;
  waiterName?: string;
  createdAt: string;
  items: OrderItem[];
}

export interface TableAccount {
  id: string;
  tableId: string;
  tableName: string;
  status: TableAccountStatus;
  total: number;
  openedAt: string;
  closedAt?: string;
  orders: Order[];
}

export interface SalesReportRow {
  productId: string;
  productName: string;
  category?: string;
  quantitySold: number;
  totalRevenue: number;
}
