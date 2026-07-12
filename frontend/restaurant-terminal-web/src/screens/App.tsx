import { useEffect, useMemo, useState } from "react";
import type { FormEvent } from "react";
import { ChefHat, ClipboardList, CreditCard, LogOut, Plus, ReceiptText, Save, Send, Utensils } from "lucide-react";
import { api } from "../services/api";
import { connectRealtime } from "../services/realtime";
import type { Category, DraftItem, Order, OrderItem, OrderItemStatus, Product, SalesReportRow, Table, TableAccount } from "../types";

type View = "waiter" | "kitchen" | "reports";
type TableFilter = "all" | "to-deliver" | "to-pay" | "paid" | "finished";
type EntrySession = { waiterName: string };

const restaurantEntryKey = "1111";

export function App() {
  const [session, setSession] = useState<EntrySession | null>(null);
  const [view, setView] = useState<View>("waiter");
  const [categories, setCategories] = useState<Category[]>([]);
  const [products, setProducts] = useState<Product[]>([]);
  const [tables, setTables] = useState<Table[]>([]);
  const [selectedCategoryId, setSelectedCategoryId] = useState<string>("");
  const [selectedTableId, setSelectedTableId] = useState<string>("");
  const [draft, setDraft] = useState<DraftItem[]>([]);
  const [orders, setOrders] = useState<Order[]>([]);
  const [account, setAccount] = useState<TableAccount | null>(null);
  const [accountsByTable, setAccountsByTable] = useState<Record<string, TableAccount | null>>({});
  const [tableNameDraft, setTableNameDraft] = useState("");
  const [tableFilter, setTableFilter] = useState<TableFilter>("all");
  const [newTableName, setNewTableName] = useState("");
  const [reportRows, setReportRows] = useState<SalesReportRow[]>([]);

  useEffect(() => {
    if (!session) return;
    void Promise.all([api.categories(), api.products(), api.tables(), api.kitchenQueue()]).then(
      ([loadedCategories, loadedProducts, loadedTables, queue]) => {
        setCategories(loadedCategories);
        setProducts(loadedProducts);
        setTables(loadedTables);
        setOrders(dedupeOrders(queue));
        setSelectedCategoryId(loadedCategories[0]?.id ?? "");
        setSelectedTableId(loadedTables[0]?.id ?? "");
        setTableNameDraft(loadedTables[0]?.name ?? "");
        void loadTableAccounts(loadedTables);
      }
    );
  }, [session]);

  useEffect(() => {
    if (!session) return;
    if (!selectedTableId) return;
    const table = tables.find((item) => item.id === selectedTableId);
    setTableNameDraft(table?.name ?? "");
    void loadSelectedAccount(selectedTableId);
  }, [session, selectedTableId, tables]);

  const filteredTables = useMemo(
    () => tables.filter((table) => matchesTableFilter(accountsByTable[table.id], tableFilter)),
    [tables, accountsByTable, tableFilter]
  );

  useEffect(() => {
    if (!session) return;
    if (filteredTables.length === 0) {
      setSelectedTableId("");
      setAccount(null);
      return;
    }

    if (!filteredTables.some((table) => table.id === selectedTableId)) {
      setSelectedTableId(filteredTables[0].id);
    }
  }, [session, filteredTables, selectedTableId]);

  useEffect(() => {
    if (!session) return;
    return connectRealtime({
      onOrderCreated: (order) => setOrders((current) => upsertOrder(current, order)),
      onStatusChanged: (item) => {
        setOrders((current) => applyStatusChangeToQueue(current, item));
        setAccount((current) => current ? updateAccountItem(current, item) : current);
        setAccountsByTable((current) => updateAccountMapItem(current, item));
      },
      onOrderCompleted: (order) => {
        setOrders((current) => current.filter((item) => item.id !== order.id));
        setAccount((current) => current ? upsertAccountOrder(current, order) : current);
        setAccountsByTable((current) => upsertAccountMapOrder(current, order));
      },
    });
  }, [session]);

  const activeProducts = products.filter((product) => product.isActive && product.categoryId === selectedCategoryId);
  const draftTotal = useMemo(() => draft.reduce((sum, item) => sum + item.product.price * item.quantity, 0), [draft]);
  const accountTotal = account?.total ?? 0;

  async function loadSelectedAccount(tableId = selectedTableId) {
    if (!tableId) return;
    const loaded = await api.tableAccount(tableId);
    setAccount(loaded);
    setAccountsByTable((current) => ({ ...current, [tableId]: loaded }));
  }

  async function loadTableAccounts(tableList = tables) {
    const pairs = await Promise.all(tableList.map(async (table) => [table.id, await api.tableAccount(table.id)] as const));
    setAccountsByTable(Object.fromEntries(pairs));
  }

  function addProduct(product: Product) {
    setDraft((current) => {
      const existing = current.find((item) => item.product.id === product.id);
      if (existing) {
        return current.map((item) =>
          item.product.id === product.id ? { ...item, quantity: item.quantity + 1 } : item
        );
      }
      return [...current, { product, quantity: 1, notes: "" }];
    });
  }

  async function sendOrder() {
    if (!session || !selectedTableId || draft.length === 0) return;
    const order = await api.createOrder(
      selectedTableId,
      session.waiterName,
      draft.map((item) => ({ productId: item.product.id, quantity: item.quantity, notes: item.notes }))
    );
    setOrders((current) => upsertOrder(current, order));
    setDraft([]);
    await loadSelectedAccount(selectedTableId);
  }

  async function createTable() {
    if (!newTableName.trim()) return;
    const table = await api.createTable(newTableName.trim(), 1);
    setTables((current) => [...current, table].sort((a, b) => a.name.localeCompare(b.name)));
    setAccountsByTable((current) => ({ ...current, [table.id]: null }));
    setSelectedTableId(table.id);
    setTableFilter("all");
    setNewTableName("");
  }

  async function saveTableName() {
    const table = tables.find((item) => item.id === selectedTableId);
    if (!table || !tableNameDraft.trim()) return;
    const updated = await api.updateTable({ ...table, name: tableNameDraft.trim() });
    setTables((current) => current.map((item) => item.id === updated.id ? updated : item));
    await loadSelectedAccount(updated.id);
  }

  async function markPaid() {
    if (!selectedTableId || !account) return;
    const paid = await api.payTable(selectedTableId);
    setAccount(paid);
    setAccountsByTable((current) => ({ ...current, [selectedTableId]: paid }));
  }

  function leaveTerminal() {
    setSession(null);
    setView("waiter");
    setCategories([]);
    setProducts([]);
    setTables([]);
    setSelectedCategoryId("");
    setSelectedTableId("");
    setDraft([]);
    setOrders([]);
    setAccount(null);
    setAccountsByTable({});
    setTableNameDraft("");
    setTableFilter("all");
    setReportRows([]);
  }

  if (!session) {
    return <EntryScreen onEnter={setSession} />;
  }

  return (
    <main>
      <aside>
        <div className="brand">
          <Utensils size={26} />
          <span>Restaurant Terminal</span>
        </div>
        <div className="session-box">
          <span>{session.waiterName}</span>
          <button onClick={leaveTerminal} title="Leave terminal" aria-label="Leave terminal">
            <LogOut size={16} />
          </button>
        </div>
        <button className={view === "waiter" ? "active" : ""} onClick={() => setView("waiter")}>
          <ClipboardList size={18} /> Waiter
        </button>
        <button className={view === "kitchen" ? "active" : ""} onClick={() => setView("kitchen")}>
          <ChefHat size={18} /> Kitchen
        </button>
        <button className={view === "reports" ? "active" : ""} onClick={() => setView("reports")}>
          <ReceiptText size={18} /> Reports
        </button>
      </aside>

      {view === "waiter" && (
        <section className="workspace">
          <div className="waiter-controls">
            <div className="segmented" role="tablist" aria-label="Table filters">
              {tableFilters.map((filter) => (
                <button
                  key={filter.value}
                  className={tableFilter === filter.value ? "active" : ""}
                  onClick={() => setTableFilter(filter.value)}
                  type="button"
                >
                  {filter.label}
                </button>
              ))}
            </div>
            <div className="new-table">
              <input
                value={newTableName}
                onChange={(event) => setNewTableName(event.target.value)}
                placeholder="New table"
                aria-label="New table name"
              />
              <button onClick={createTable} title="Add table" aria-label="Add table">
                <Plus size={18} />
              </button>
            </div>
          </div>
          <div className="table-picker" aria-label="Tables">
            {filteredTables.map((table) => (
              <button
                key={table.id}
                className={selectedTableId === table.id ? "active" : ""}
                onClick={() => setSelectedTableId(table.id)}
                type="button"
              >
                {table.name}
              </button>
            ))}
          </div>
          <header className="toolbar">
            <input
              className="table-name-input"
              value={tableNameDraft}
              onChange={(event) => setTableNameDraft(event.target.value)}
              aria-label="Table name"
            />
            <button onClick={saveTableName} title="Save table name" aria-label="Save table name">
              <Save size={18} />
            </button>
            <strong>Account: EUR {accountTotal.toFixed(2)} - {account?.status ?? "No account"}</strong>
            <strong>Draft: EUR {draftTotal.toFixed(2)}</strong>
            <button className="primary" onClick={sendOrder}>
              <Send size={18} /> Send
            </button>
          </header>
          <div className="tabs">
            {categories.map((category) => (
              <button
                key={category.id}
                className={selectedCategoryId === category.id ? "active" : ""}
                onClick={() => setSelectedCategoryId(category.id)}
              >
                {category.name}
              </button>
            ))}
          </div>
          <div className="content-grid">
            <div className="products">
              {activeProducts.map((product) => (
                <button className="product-card" key={product.id} onClick={() => addProduct(product)}>
                  <img src={product.photoUrl || "/placeholder-food.svg"} alt={product.photoAltText || product.name} />
                  <span>{product.name}</span>
                  <strong>EUR {product.price.toFixed(2)}</strong>
                </button>
              ))}
            </div>
            <div className="side-stack">
              <OrderDraft draft={draft} setDraft={setDraft} />
              <TableAccountPanel account={account} onPaid={markPaid} />
            </div>
          </div>
        </section>
      )}

      {view === "kitchen" && (
        <KitchenQueue
          orders={orders}
          onStatus={async (id, status) => {
            const item = await api.updateStatus(id, status);
            setOrders((current) => applyStatusChangeToQueue(current, item));
          }}
          onDone={async (orderId) => {
            await api.completeOrder(orderId);
            setOrders((current) => current.filter((order) => order.id !== orderId));
          }}
        />
      )}
      {view === "reports" && <Reports rows={reportRows} onRows={setReportRows} />}
    </main>
  );
}

function EntryScreen({ onEnter }: { onEnter: (session: EntrySession) => void }) {
  const [waiterName, setWaiterName] = useState("");
  const [entryKey, setEntryKey] = useState("");
  const [error, setError] = useState("");

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!waiterName.trim()) {
      setError("Enter your name.");
      return;
    }

    if (entryKey !== restaurantEntryKey) {
      setError("Invalid restaurant key.");
      return;
    }

    onEnter({ waiterName: waiterName.trim() });
  }

  return (
    <main className="entry-page">
      <form className="entry-panel" onSubmit={submit}>
        <div className="entry-brand">
          <Utensils size={30} />
          <h1>Restaurant Terminal</h1>
        </div>
        <label>
          Waiter name
          <input value={waiterName} onChange={(event) => setWaiterName(event.target.value)} autoFocus />
        </label>
        <label>
          Restaurant key
          <input value={entryKey} onChange={(event) => setEntryKey(event.target.value)} inputMode="numeric" />
        </label>
        {error && <p className="entry-error">{error}</p>}
        <button className="entry-submit" type="submit">Enter</button>
      </form>
    </main>
  );
}

function OrderDraft({ draft, setDraft }: { draft: DraftItem[]; setDraft: (items: DraftItem[]) => void }) {
  const orderedDraft = [...draft].sort((a, b) => compareCategories(a.product.categoryName, b.product.categoryName) || a.product.name.localeCompare(b.product.name));

  return (
    <section className="panel">
      <h2>Current order</h2>
      {orderedDraft.map((item) => (
        <label className="draft-item" key={item.product.id}>
          <span>{item.quantity} x {item.product.name}</span>
          <small>{item.product.categoryName}</small>
          <input
            value={item.notes}
            placeholder="Notes"
            onChange={(event) =>
              setDraft(draft.map((existing) => existing.product.id === item.product.id ? { ...existing, notes: event.target.value } : existing))
            }
          />
        </label>
      ))}
    </section>
  );
}

function TableAccountPanel({ account, onPaid }: { account: TableAccount | null; onPaid: () => void }) {
  const items = account?.orders.flatMap((order) => order.items.map((item) => ({ ...item, orderCreatedAt: order.createdAt }))) ?? [];
  const orderedItems = items.sort((a, b) => compareCategories(a.categoryName, b.categoryName) || a.productName.localeCompare(b.productName));
  const groups = orderedItems.reduce<Record<string, typeof orderedItems>>((current, item) => {
    const key = item.categoryName || "Other";
    current[key] = [...(current[key] ?? []), item];
    return current;
  }, {});
  const groupNames = Object.keys(groups).sort(compareCategories);

  return (
    <section className="panel">
      <div className="panel-header">
        <h2>{account?.tableName ?? "Table account"}</h2>
        {account && <span className={`status-pill ${account.status.toLowerCase()}`}>{account.status}</span>}
      </div>
      {!account && <p className="empty-state">No requests yet.</p>}
      {account && (
        <>
          <div className="account-summary">
            <strong>EUR {account.total.toFixed(2)}</strong>
            <button className="pay-button" onClick={onPaid} disabled={account.status === "Paid"}>
              <CreditCard size={16} /> Paid
            </button>
          </div>
          {groupNames.map((category) => (
            <div className="account-group" key={category}>
              <h3>{category}</h3>
              {groups[category].map((item) => (
                <div className="account-item" key={item.id}>
                  <span>{item.quantity} x {item.productName}</span>
                  <span className={`status-pill ${item.status.toLowerCase()}`}>{item.status}</span>
                  {item.notes && <small>{item.notes}</small>}
                </div>
              ))}
            </div>
          ))}
        </>
      )}
    </section>
  );
}

function KitchenQueue({
  orders,
  onStatus,
  onDone
}: {
  orders: Order[];
  onStatus: (id: string, status: OrderItemStatus) => void;
  onDone: (orderId: string) => void;
}) {
  return (
    <section className="workspace">
      <header className="toolbar"><h1>Kitchen queue</h1></header>
      <div className="queue">
        {orders.map((order) => (
          <article className="order-card" key={order.id}>
            <div className="order-card-header">
              <div>
                <h2>{order.tableName}</h2>
                <small className="order-sender">Sent by {order.waiterName || "Unknown waiter"}</small>
              </div>
              <select value="active" onChange={(event) => event.target.value === "done" && onDone(order.id)} aria-label={`Order action for ${order.tableName}`}>
                <option value="active">Active</option>
                <option value="done">Done</option>
              </select>
            </div>
            {order.items.map((item) => (
              <div className="queue-row" key={item.id}>
                <span>{item.quantity} x {item.productName}</span>
                <small>{item.notes}</small>
                <select value={item.status} onChange={(event) => onStatus(item.id, event.target.value as OrderItemStatus)}>
                  {["Ordered", "BeingPrepared", "Ready", "Delivered", "Cancelled"].map((status) => (
                    <option key={status}>{status}</option>
                  ))}
                </select>
              </div>
            ))}
          </article>
        ))}
      </div>
    </section>
  );
}

function Reports({ rows, onRows }: { rows: SalesReportRow[]; onRows: (rows: SalesReportRow[]) => void }) {
  const [from, setFrom] = useState("2026-07-01T10:00");
  const [to, setTo] = useState("2026-07-06T23:00");

  async function submit() {
    onRows(await api.sales(new Date(from).toISOString(), new Date(to).toISOString()));
  }

  return (
    <section className="workspace">
      <header className="toolbar">
        <input type="datetime-local" value={from} onChange={(event) => setFrom(event.target.value)} />
        <input type="datetime-local" value={to} onChange={(event) => setTo(event.target.value)} />
        <button className="primary" onClick={submit}>Run</button>
      </header>
      <table>
        <thead><tr><th>Product</th><th>Category</th><th>Qty</th><th>Revenue</th></tr></thead>
        <tbody>
          {rows.map((row) => (
            <tr key={row.productId}><td>{row.productName}</td><td>{row.category}</td><td>{row.quantitySold}</td><td>EUR {row.totalRevenue.toFixed(2)}</td></tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}

function compareCategories(left?: string, right?: string) {
  const leftRank = categoryRank(left);
  const rightRank = categoryRank(right);
  if (leftRank !== rightRank) return leftRank - rightRank;
  return (left ?? "").localeCompare(right ?? "");
}

function categoryRank(category?: string) {
  return category?.toLowerCase() === "drinks" ? 1 : 0;
}

function updateAccountItem(account: TableAccount, item: OrderItem): TableAccount {
  return {
    ...account,
    orders: account.orders.map((order) => ({
      ...order,
      items: order.items.map((existing) => existing.id === item.id ? item : existing)
    }))
  };
}

function updateAccountMapItem(accounts: Record<string, TableAccount | null>, item: OrderItem) {
  return Object.fromEntries(Object.entries(accounts).map(([tableId, account]) => [
    tableId,
    account && account.orders.some((order) => order.items.some((existing) => existing.id === item.id))
      ? updateAccountItem(account, item)
      : account
  ]));
}

function upsertAccountOrder(account: TableAccount, order: Order): TableAccount {
  return {
    ...account,
    orders: [order, ...account.orders.filter((item) => item.id !== order.id)]
  };
}

function upsertAccountMapOrder(accounts: Record<string, TableAccount | null>, order: Order) {
  return Object.fromEntries(Object.entries(accounts).map(([tableId, account]) => [
    tableId,
    account?.tableId === order.tableId ? upsertAccountOrder(account, order) : account
  ]));
}

function upsertOrder(orders: Order[], order: Order): Order[] {
  if (!hasKitchenWork(order)) {
    return orders.filter((item) => item.id !== order.id);
  }

  return dedupeOrders([order, ...orders.filter((item) => item.id !== order.id)]);
}

function dedupeOrders(orders: Order[]): Order[] {
  const seen = new Set<string>();
  return orders.filter((order) => {
    if (seen.has(order.id) || !hasKitchenWork(order)) return false;
    seen.add(order.id);
    return true;
  });
}

function applyStatusChangeToQueue(orders: Order[], item: OrderItem): Order[] {
  return dedupeOrders(orders.map((order) => {
    if (!order.items.some((existing) => existing.id === item.id)) return order;
    return {
      ...order,
      items: order.items
        .map((existing) => existing.id === item.id ? item : existing)
        .filter((existing) => isKitchenWork(existing.status))
    };
  }));
}

function hasKitchenWork(order: Order) {
  return order.items.some((item) => isKitchenWork(item.status));
}

function isKitchenWork(status: OrderItemStatus) {
  return status !== "Delivered" && status !== "Cancelled";
}

const tableFilters: Array<{ value: TableFilter; label: string }> = [
  { value: "all", label: "All" },
  { value: "to-deliver", label: "To deliver" },
  { value: "to-pay", label: "To pay" },
  { value: "paid", label: "Paid" },
  { value: "finished", label: "Finished" }
];

function matchesTableFilter(account: TableAccount | null | undefined, filter: TableFilter) {
  if (filter === "all") return true;
  if (!account) return filter === "to-deliver";

  const delivered = isAccountDelivered(account);
  const paid = account.status === "Paid";

  if (filter === "to-deliver") return !delivered;
  if (filter === "to-pay") return delivered && !paid;
  if (filter === "paid") return paid;
  return paid && delivered;
}

function isAccountDelivered(account: TableAccount) {
  const items = account.orders.flatMap((order) => order.items);
  return items.length > 0 && items.every((item) => item.status === "Delivered" || item.status === "Cancelled");
}
