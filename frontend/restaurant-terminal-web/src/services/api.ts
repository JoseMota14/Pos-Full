import type { Category, Order, OrderItem, OrderItemStatus, Product, SalesReportRow, Table, TableAccount } from "../types";

const jsonHeaders = { "Content-Type": "application/json" };

async function request<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await fetch(url, init);
  if (!response.ok) {
    throw new Error(await response.text());
  }
  return response.json() as Promise<T>;
}

export const api = {
  categories: () => request<Category[]>("/api/categories"),
  products: () => request<Product[]>("/api/products"),
  tables: () => request<Table[]>("/api/tables"),
  createTable: (name: string, seats: number) =>
    request<Table>("/api/tables", {
      method: "POST",
      headers: jsonHeaders,
      body: JSON.stringify({ name, seats })
    }),
  updateTable: (table: Table) =>
    request<Table>(`/api/tables/${table.id}`, {
      method: "PUT",
      headers: jsonHeaders,
      body: JSON.stringify({ name: table.name, seats: table.seats })
    }),
  tableAccount: async (tableId: string) => {
    const response = await fetch(`/api/tables/${tableId}/account`);
    if (response.status === 404) return null;
    if (!response.ok) throw new Error(await response.text());
    return response.json() as Promise<TableAccount>;
  },
  payTable: (tableId: string) => request<TableAccount>(`/api/tables/${tableId}/pay`, { method: "POST" }),
  createOrder: (tableId: string, waiterName: string, items: Array<{ productId: string; quantity: number; notes?: string }>) =>
    request<Order>("/api/orders", {
      method: "POST",
      headers: jsonHeaders,
      body: JSON.stringify({ tableId, waiterName, items })
    }),
  kitchenQueue: () => request<Order[]>("/api/orders/kitchen-queue"),
  updateStatus: (itemId: string, status: OrderItemStatus) =>
    request<OrderItem>(`/api/orders/items/${itemId}/status`, {
      method: "PUT",
      headers: jsonHeaders,
      body: JSON.stringify({ status })
    }),
  completeOrder: (orderId: string) => request<Order>(`/api/orders/${orderId}/complete`, {
    method: "POST",
    headers: jsonHeaders,
    body: JSON.stringify({})
  }),
  sales: (from: string, to: string) => request<SalesReportRow[]>(`/api/reports/sales?from=${from}&to=${to}`)
};
