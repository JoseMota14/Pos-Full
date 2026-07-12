import * as signalR from "@microsoft/signalr";
import type { Order, OrderItem } from "../types";

export function connectRealtime(handlers: {
  onOrderCreated?: (order: Order) => void;
  onStatusChanged?: (item: OrderItem) => void;
  onOrderCompleted?: (order: Order) => void;
}) {
  const orders = new signalR.HubConnectionBuilder().withUrl("/hubs/orders").withAutomaticReconnect().build();
  orders.on("OrderCreated", (order: Order) => handlers.onOrderCreated?.(order));
  orders.on("OrderItemStatusChanged", (item: OrderItem) => handlers.onStatusChanged?.(item));
  orders.on("OrderCompleted", (order: Order) => handlers.onOrderCompleted?.(order));
  void orders.start();
  return () => void orders.stop();
}
