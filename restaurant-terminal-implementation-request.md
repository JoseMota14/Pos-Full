# Restaurant Terminal Implementation Request

## Objective

Build a restaurant terminal/POS prototype for managing products, table orders, kitchen workflow, and sales reports.

The system should support multiple waiter terminals and one or more kitchen displays connected to the same backend. It does not need offline support for the first version, but it must be designed so it can grow vertically with more features later.

## Constraints

- Backend must be written in .NET / C#.
- The implementation should use free and MIT-compatible technologies where possible.
- Avoid paid cloud services for the first version.
- Future hosting must preserve a zero-recurring-cost option.
- The application should run locally or on a self-hosted machine inside the restaurant.
- Offline mode is not required for the first implementation.
- Do not implement voice ordering in the first version, but keep the architecture flexible enough to add it later.

## Preferred Stack

### Backend

- ASP.NET Core Web API
- C#
- SignalR for realtime updates
- Entity Framework Core
- SQLite for the first database

### Frontend

- React
- Vite
- TypeScript
- Tailwind CSS or another free permissive UI approach
- SignalR client

### Deployment

- Local/self-hosted backend server
- Browser-based terminals for waiters, kitchen, and admin

### Future Hosting

- The primary hosting path should be self-hosting on hardware the restaurant already owns.
- Do not require paid cloud services, paid databases, paid realtime infrastructure, or paid storage.
- Avoid architecture choices that make the app dependent on a specific cloud provider.
- Cloud free tiers can be documented as optional experiments, but they must not be required because free-tier terms can change.
- The app should be deployable as a normal ASP.NET Core service with a built frontend.
- The backend should be able to serve the frontend static files directly, so one process can host the whole app.
- The database should remain local/self-hosted by default.
- Product images should be stored locally by default, with a media abstraction that can support external storage later.
- Configuration should be environment-based so the app can move from a restaurant PC to a VPS or container later without code changes.

Preferred zero-cost hosting options:

- Restaurant PC or mini-PC on the local network.
- Existing Windows machine running the ASP.NET Core app as a service.
- Existing Linux machine running the ASP.NET Core app with systemd.
- Existing NAS or home/server hardware if it can run .NET reliably.
- Docker on existing hardware, if Docker is already available.

Optional future hosting options, only if they stay free:

- Free static hosting for the frontend plus self-hosted backend.
- Free-tier VPS or app hosting.
- Free managed database.

These optional free-tier options should be treated as non-guaranteed. The guaranteed zero-recurring-cost architecture is local/self-hosted.

### Testing

- xUnit for .NET backend unit and integration tests
- WebApplicationFactory for ASP.NET Core API integration tests
- EF Core SQLite test database for integration tests
- Vitest and React Testing Library for frontend unit/component tests

## Core Interfaces

### Waiter Terminal

The waiter interface should allow users to:

- Log in.
- Select a restaurant table.
- Browse products by tabs/categories such as soups, plates, drinks, desserts, and others.
- See product photos while browsing products.
- Add products to an order.
- Add quantity and optional notes per item.
- Send the order to the kitchen or bar workflow.
- See the current state of each ordered item.
- See the current table account total.
- Mark items as delivered when appropriate.
- Close the table account after payment.

### Kitchen Interface

The kitchen interface should allow kitchen staff to:

- See incoming order items in realtime.
- Group items by table and order.
- See item notes.
- Mark items as being prepared.
- Mark items as ready.
- Keep a clear queue of pending work.

### Admin Interface

The admin interface should allow an administrator to:

- Create, update, deactivate, and list products.
- Add and update product photos.
- Create and manage product categories.
- Create and manage restaurant tables.
- Create and manage users.
- Assign user roles.
- View sales reports.
- Export articles sold within a selected time frame.

## Roles

Initial roles:

- Admin
- Waiter
- Kitchen

Future roles may include:

- Manager
- Bar
- Cashier

## Core Domain Concepts

### Products

Products belong to categories and have a price. Products should have a photo so waiters can identify items quickly in the terminal UI. Products may optionally be routed to the kitchen or bar.

The first version can support one main product photo per product. The data model should remain flexible enough to support multiple photos, thumbnails, image variants, or externally hosted media later.

Example categories:

- Soups
- Plates
- Drinks
- Desserts

### Tables

Each restaurant table can have an open account. Multiple orders may be attached to the same open table account.

### Orders

An order contains one or more order items. Each item should have its own status so that one item can be ready while another is still being prepared.

### Order Statuses

Use statuses similar to:

- Ordered
- BeingPrepared
- Ready
- Delivered
- Cancelled

### Table Account Statuses

Use statuses similar to:

- Open
- Closed
- Paid
- Cancelled

## Suggested Database Tables

Use names appropriate to the implementation style, but the model should include:

- Users
- Roles
- RestaurantTables
- ProductCategories
- Products
- ProductImages
- Orders
- OrderItems
- OrderStatusEvents
- Payments

## Suggested Backend Structure

```txt
RestaurantTerminal.Api/
  Controllers/
    AuthController.cs
    ProductsController.cs
    CategoriesController.cs
    TablesController.cs
    OrdersController.cs
    ReportsController.cs

  Hubs/
    KitchenHub.cs
    OrdersHub.cs

  Data/
    AppDbContext.cs

  Models/
    User.cs
    Role.cs
    RestaurantTable.cs
    ProductCategory.cs
    Product.cs
    ProductImage.cs
    Order.cs
    OrderItem.cs
    OrderStatusEvent.cs
    Payment.cs

  Services/
    OrderService.cs
    ProductService.cs
    ReportService.cs

RestaurantTerminal.Api.Tests/
  Unit/
    OrderServiceTests.cs
    ProductServiceTests.cs
    ReportServiceTests.cs

  Integration/
    OrdersApiTests.cs
    ProductsApiTests.cs
    ReportsApiTests.cs
    RealtimeStatusFlowTests.cs

restaurant-terminal-web/
  src/
    components/
    screens/
    services/
    tests/
```

## Realtime Requirements

Use SignalR so screens update without manual refresh.

When a waiter sends an order:

```txt
Waiter creates order
Backend saves order and order items
Backend broadcasts new kitchen items
Kitchen interface updates immediately
```

When kitchen changes an item status:

```txt
Kitchen marks item as BeingPrepared or Ready
Backend saves the status change
Backend broadcasts the update
Waiter interface updates immediately
```

## Reporting Requirements

The system must be able to extract articles bought in a specific time frame.

Reports should support:

- Start date/time
- End date/time
- Product name
- Category
- Quantity sold
- Total revenue
- Optional waiter filter
- Optional table filter

Export formats:

- CSV for first version
- XLSX can be added later

Example endpoint:

```txt
GET /api/reports/sales?from=2026-07-01T10:00:00&to=2026-07-06T23:00:00
GET /api/reports/sales/export.csv?from=2026-07-01T10:00:00&to=2026-07-06T23:00:00
```

## Testing Requirements

The implementation must include automated tests from the first version.

### Backend Unit Tests

Backend unit tests should cover domain and service behavior without requiring the full application server.

Required coverage:

- Product creation and validation.
- Product category behavior.
- Product photo metadata behavior.
- Order creation rules.
- Order item status transitions.
- Table account total calculation.
- Sales report aggregation logic.
- CSV export formatting logic.

### Backend Integration Tests

Backend integration tests should run against the real ASP.NET Core API pipeline using a test database.

Required coverage:

- Create, update, list, and deactivate products.
- Add or update product photos.
- Create product categories.
- Create restaurant tables.
- Create an order for a table.
- Retrieve an open table account.
- Update order item statuses.
- Generate sales reports by time frame.
- Export sales report as CSV.
- Verify that key order and status changes are published through the realtime layer.

### Frontend Tests

Frontend tests should cover important user-facing behavior.

Required coverage:

- Product tabs render categories correctly.
- Product cards show names, prices, and photos.
- Adding products updates the current order draft.
- Table account total updates when items are added.
- Kitchen queue renders grouped order items.
- Status changes are reflected in the waiter interface.
- Report date filters submit the correct request.

### Test Quality Expectations

- Tests should be runnable with a normal local command.
- Tests should use predictable seed data.
- Integration tests should not depend on production data.
- Avoid tests that require paid services or external network access.
- Add tests for bug fixes as the system grows.
- Keep business rules testable outside controllers and UI components.

## Future Growth Requirements

The first version should remain simple, but the architecture should leave room for:

- Voice order input
- Multiple product images and media variants
- Inventory and stock control
- Reservations
- Bar-specific display
- Receipt printing
- Fiscal/tax integration
- Payment provider integration
- Multi-restaurant support
- Customer loyalty
- Takeaway and delivery orders
- Kitchen preparation time analytics

## Future Voice Ordering Note

Voice ordering should not be implemented in the first version.

However, the order creation flow should be designed so that future input methods can create an order draft.

Future voice flow:

```txt
Waiter speaks request
Speech is converted to text
Text is parsed into an order draft
Waiter reviews and confirms
Confirmed draft becomes a normal order
```

Voice should never send an order directly without waiter confirmation.

## Implementation Priorities

Build in this order:

1. Backend project structure.
2. Backend test project structure.
3. Database models and migrations.
4. Product/category management, including product photos.
5. Product/category tests.
6. Table management.
7. Waiter order creation screen.
8. Order creation and table account tests.
9. Kitchen realtime queue.
10. Realtime/status integration tests.
11. Order item status updates.
12. Table account total and closing flow.
13. Sales report by time frame.
14. Report and CSV export tests.
15. CSV export.

## Definition Of Done For First Prototype

The first prototype is complete when:

- An admin can create products and categories.
- An admin can add a photo to each product.
- A waiter can select a table and create an order.
- Products are selectable through category tabs.
- Products show photos in the waiter terminal.
- The kitchen screen receives new order items in realtime.
- Kitchen staff can update item status.
- The waiter screen shows updated statuses.
- The table account total is visible.
- A table account can be closed.
- Sales can be extracted for a selected time frame.
- A CSV export can be generated.
- Backend unit tests exist for product, order, table account, and reporting logic.
- Backend integration tests exist for product, order, kitchen status, report, and export flows.
- Frontend tests exist for the main waiter, kitchen, product, and reporting interactions.
- The full test suite can be run locally without paid services or external network access.
