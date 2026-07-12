using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RestaurantTerminal.Api.Contracts;
using RestaurantTerminal.Api.Data;
using RestaurantTerminal.Api.Models;

namespace RestaurantTerminal.Api.Services;

public sealed class ReportService(AppDbContext db)
{
    public async Task<IReadOnlyList<SalesReportRow>> GetSalesAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        Guid? waiterId = null,
        Guid? tableId = null,
        CancellationToken cancellationToken = default)
    {
        if (to < from)
        {
            throw new InvalidOperationException("The report end date must be after the start date.");
        }

        var query = db.OrderItems
            .Include(x => x.Order)!.ThenInclude(x => x!.TableAccount)
            .Where(x => x.Status != OrderItemStatus.Cancelled);

        if (waiterId is not null)
        {
            query = query.Where(x => x.Order!.WaiterId == waiterId);
        }

        if (tableId is not null)
        {
            query = query.Where(x => x.Order!.TableAccount!.RestaurantTableId == tableId);
        }

        var items = await query.ToListAsync(cancellationToken);

        return items
            .Where(x => x.CreatedAt >= from && x.CreatedAt <= to)
            .GroupBy(x => new { x.ProductId, x.ProductName, x.CategoryName })
            .Select(x => new SalesReportRow(
                x.Key.ProductId,
                x.Key.ProductName,
                x.Key.CategoryName,
                x.Sum(i => i.Quantity),
                x.Sum(i => i.Quantity * i.UnitPrice)))
            .OrderBy(x => x.ProductName)
            .ToList();
    }

    public string ToCsv(IEnumerable<SalesReportRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("ProductId,ProductName,Category,QuantitySold,TotalRevenue");
        foreach (var row in rows)
        {
            builder.Append(Csv(row.ProductId.ToString()));
            builder.Append(',');
            builder.Append(Csv(row.ProductName));
            builder.Append(',');
            builder.Append(Csv(row.Category ?? string.Empty));
            builder.Append(',');
            builder.Append(row.QuantitySold.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.AppendLine(row.TotalRevenue.ToString("0.00", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private static string Csv(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
