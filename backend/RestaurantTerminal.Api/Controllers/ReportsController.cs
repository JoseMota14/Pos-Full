using Microsoft.AspNetCore.Mvc;
using RestaurantTerminal.Api.Contracts;
using RestaurantTerminal.Api.Services;

namespace RestaurantTerminal.Api.Controllers;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController(ReportService reports) : ControllerBase
{
    [HttpGet("sales")]
    public async Task<IReadOnlyList<SalesReportRow>> Sales(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        [FromQuery] Guid? waiterId,
        [FromQuery] Guid? tableId,
        CancellationToken cancellationToken)
    {
        return await reports.GetSalesAsync(from, to, waiterId, tableId, cancellationToken);
    }

    [HttpGet("sales/export.csv")]
    public async Task<FileContentResult> ExportSalesCsv(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        [FromQuery] Guid? waiterId,
        [FromQuery] Guid? tableId,
        CancellationToken cancellationToken)
    {
        var rows = await reports.GetSalesAsync(from, to, waiterId, tableId, cancellationToken);
        var csv = reports.ToCsv(rows);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "sales-report.csv");
    }
}
