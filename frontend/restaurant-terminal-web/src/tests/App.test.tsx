import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { App } from "../screens/App";

vi.mock("../services/realtime", () => ({ connectRealtime: () => () => undefined }));

const categoryId = "cat-1";
const drinkCategoryId = "cat-2";
const tableId = "table-1";
const productId = "product-1";

function mockFetch() {
  globalThis.fetch = vi.fn(async (url: RequestInfo | URL, init?: RequestInit) => {
    const path = String(url);
    if (path === "/api/categories") {
      return json([
        { id: categoryId, name: "Soups", sortOrder: 1, isActive: true },
        { id: drinkCategoryId, name: "Drinks", sortOrder: 2, isActive: true }
      ]);
    }
    if (path === "/api/products") {
      return json([{ id: productId, name: "Chicken Soup", price: 12.5, categoryId, categoryName: "Soups", photoUrl: "/soup.jpg", isActive: true }]);
    }
    if (path === "/api/tables" && init?.method === "POST") {
      return json({ id: "table-2", name: "Table 2", seats: 2, isActive: true });
    }
    if (path === "/api/tables") {
      return json([{ id: tableId, name: "Table 1", seats: 4, isActive: true }]);
    }
    if (path === `/api/tables/${tableId}/account`) {
      return json({
        id: "account-1",
        tableId,
        tableName: "Table 1",
        status: "Open",
        total: 14.5,
        openedAt: new Date().toISOString(),
        orders: [{
          id: "order-existing",
          tableAccountId: "account-1",
          tableId,
          tableName: "Table 1",
          waiterName: "Ana",
          createdAt: new Date().toISOString(),
          items: [
            { id: "soup-item", orderId: "order-existing", productId: "soup", productName: "Chicken Soup", categoryName: "Soups", quantity: 1, unitPrice: 12.5, status: "BeingPrepared", createdAt: new Date().toISOString() },
            { id: "drink-item", orderId: "order-existing", productId: "drink", productName: "Orange Juice", categoryName: "Drinks", quantity: 1, unitPrice: 2, status: "Delivered", createdAt: new Date().toISOString() }
          ]
        }]
      });
    }
    if (path === "/api/orders/kitchen-queue") {
      return json([{
        id: "order-existing",
        tableAccountId: "account-1",
        tableId,
        tableName: "Table 1",
        waiterName: "Ana",
        createdAt: new Date().toISOString(),
        items: [
          { id: "soup-item", orderId: "order-existing", productId: "soup", productName: "Chicken Soup", categoryName: "Soups", quantity: 1, unitPrice: 12.5, status: "BeingPrepared", createdAt: new Date().toISOString() }
        ]
      }]);
    }
    if (path === "/api/orders" && init?.method === "POST") {
      return json({ id: "order-1", tableAccountId: "account-1", tableId, tableName: "Table 1", waiterName: "Ana", createdAt: new Date().toISOString(), items: [] });
    }
    if (path.startsWith("/api/reports/sales")) {
      return json([{ productId, productName: "Steak", category: "Plates", quantitySold: 2, totalRevenue: 25 }]);
    }
    return new Response("", { status: 404 });
  }) as typeof fetch;
}

describe("App", () => {
  it("renders product tabs, cards, draft totals, and report requests", async () => {
    mockFetch();
    const user = userEvent.setup();
    render(<App />);

    expect(screen.getByRole("button", { name: "Enter" })).toBeInTheDocument();
    await user.type(screen.getByLabelText("Waiter name"), "Ana");
    await user.type(screen.getByLabelText("Restaurant key"), "0000");
    await user.click(screen.getByRole("button", { name: "Enter" }));
    expect(screen.getByText("Invalid restaurant key.")).toBeInTheDocument();

    await user.clear(screen.getByLabelText("Restaurant key"));
    await user.type(screen.getByLabelText("Restaurant key"), "1111");
    await user.click(screen.getByRole("button", { name: "Enter" }));

    expect(await screen.findByRole("button", { name: "Soups" })).toBeInTheDocument();
    expect(screen.getByText("Ana")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Chicken Soup/ })).toBeInTheDocument();
    expect(await screen.findByText("BeingPrepared")).toBeInTheDocument();
    expect(screen.getByText("Delivered")).toBeInTheDocument();
    expect(screen.getByText("Account: EUR 14.50 - Open")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Finished" })).toBeInTheDocument();

    await user.type(screen.getByLabelText("New table name"), "Table 2");
    await user.click(screen.getByRole("button", { name: "Add table" }));
    expect(await screen.findByRole("button", { name: "Table 2" })).toBeInTheDocument();
    const createTableCall = vi.mocked(fetch).mock.calls.find(([url, init]) => String(url) === "/api/tables" && init?.method === "POST");
    expect(JSON.parse(String(createTableCall?.[1]?.body))).toMatchObject({ name: "Table 2", seats: 1 });

    await user.click(screen.getByRole("button", { name: /Chicken Soup/ }));
    expect(screen.getAllByText("1 x Chicken Soup").length).toBeGreaterThan(0);
    expect(screen.getByText("Draft: EUR 12.50")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Send" }));
    const createOrderCall = vi.mocked(fetch).mock.calls.find(([url, init]) => String(url) === "/api/orders" && init?.method === "POST");
    expect(JSON.parse(String(createOrderCall?.[1]?.body))).toMatchObject({ waiterName: "Ana" });

    await user.click(screen.getByRole("button", { name: /Kitchen/ }));
    expect(screen.getByText("Sent by Ana")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /Reports/ }));
    await user.click(screen.getByRole("button", { name: "Run" }));
    await waitFor(() => expect(screen.getByText("EUR 25.00")).toBeInTheDocument());
    expect(vi.mocked(fetch).mock.calls.some(([url]) => String(url).startsWith("/api/reports/sales?from="))).toBe(true);
  });
});

function json(body: unknown) {
  return new Response(JSON.stringify(body), { status: 200, headers: { "Content-Type": "application/json" } });
}
