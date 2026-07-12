using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantTerminal.Api.Contracts;
using RestaurantTerminal.Api.Data;
using RestaurantTerminal.Api.Models;
using RestaurantTerminal.Api.Services;

namespace RestaurantTerminal.Api.Controllers;

[ApiController]
[Route("api/tables")]
public sealed class TablesController(AppDbContext db, OrderService orders) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<TableResponse>> List(CancellationToken cancellationToken)
    {
        return await db.RestaurantTables
            .OrderBy(x => x.Name)
            .Select(x => new TableResponse(x.Id, x.Name, x.Seats, x.IsActive))
            .ToListAsync(cancellationToken);
    }

    [HttpPost]
    public async Task<ActionResult<TableResponse>> Create(TableRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Table name is required.");
        }

        var table = new RestaurantTable { Name = request.Name.Trim(), Seats = request.Seats };
        db.RestaurantTables.Add(table);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(List), new TableResponse(table.Id, table.Name, table.Seats, table.IsActive));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TableResponse>> Update(Guid id, TableRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Table name is required.");
        }

        var table = await db.RestaurantTables.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (table is null)
        {
            return NotFound();
        }

        table.Name = request.Name.Trim();
        table.Seats = request.Seats;
        await db.SaveChangesAsync(cancellationToken);
        return new TableResponse(table.Id, table.Name, table.Seats, table.IsActive);
    }

    [HttpGet("{id:guid}/account")]
    public async Task<ActionResult<TableAccountResponse>> GetOpenAccount(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var account = await orders.GetCurrentAccountAsync(id, cancellationToken);
            return ToResponse(account);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult<TableAccountResponse>> Close(Guid id, CancellationToken cancellationToken)
    {
        var account = await orders.CloseAccountAsync(id, cancellationToken);
        return ToResponse(account);
    }

    [HttpPost("{id:guid}/pay")]
    public async Task<ActionResult<TableAccountResponse>> Pay(Guid id, CancellationToken cancellationToken)
    {
        var account = await orders.PayAccountAsync(id, cancellationToken);
        return ToResponse(account);
    }

    private static TableAccountResponse ToResponse(TableAccount account)
    {
        return new TableAccountResponse(
            account.Id,
            account.RestaurantTableId,
            account.RestaurantTable?.Name ?? string.Empty,
            account.Status,
            account.Total,
            account.OpenedAt,
            account.ClosedAt,
            account.Orders.Select(OrdersController.ToResponse).ToList());
    }
}
