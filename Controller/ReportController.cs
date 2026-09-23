using System.Globalization;
using Katalog.Models;
using Katalog.Services;

namespace Katalog.Controller
{
    public static class ReportController
    {
        private static readonly string[] AllowedPeriods =
            { "day", "month", "year" };

        public static void MapReport(this WebApplication app)
        {
            var report = app
                .MapGroup("api/v1/reports")
                .RequireAuthorization(Policies.AdminPetugas);

            // =========================================================
            // SALES REPORT (ADMIN & PETUGAS)
            //
            // GET /api/v1/reports/sales
            //
            // Query parameter:
            //   startDate  yyyy-MM-dd (inclusive, opsional;
            //              default: endDate - 29 hari)
            //   endDate    yyyy-MM-dd (inclusive, opsional;
            //              default: hari ini)
            //   period     day | month | year (default: day)
            //
            // Response: SalesReportResponse — summary agregat +
            // baris per periode.
            // =========================================================

            report.MapGet("/sales", async (
                ReportServices service,
                [AsParameters] SalesReportQuery query) =>
            {
                try
                {
                    var period =
                        (query.Period ?? "day").Trim().ToLowerInvariant();

                    if (!AllowedPeriods.Contains(period))
                    {
                        return Results.BadRequest(new
                        {
                            message = "Period harus salah satu dari: day, month, year"
                        });
                    }

                    if (!TryParseDate(
                            query.StartDate,
                            out var startDate))
                    {
                        return Results.BadRequest(new
                        {
                            message = "Format startDate harus yyyy-MM-dd"
                        });
                    }

                    if (!TryParseDate(
                            query.EndDate,
                            out var endDate))
                    {
                        return Results.BadRequest(new
                        {
                            message = "Format endDate harus yyyy-MM-dd"
                        });
                    }

                    var hasStart = !string.IsNullOrWhiteSpace(
                        query.StartDate);
                    var hasEnd = !string.IsNullOrWhiteSpace(
                        query.EndDate);

                    // Default: rentang 30 hari terakhir sampai hari ini.
                    if (!hasEnd)
                    {
                        endDate = DateTime.Today;
                    }

                    if (!hasStart)
                    {
                        startDate = endDate.AddDays(-29);
                    }

                    if (startDate.Date > endDate.Date)
                    {
                        return Results.BadRequest(new
                        {
                            message = "startDate tidak boleh melebihi endDate"
                        });
                    }

                    var result =
                        await service.GetSalesReportAsync(
                            startDate,
                            endDate,
                            period);

                    return Results.Ok(result);
                }
                catch (Exception e)
                {
                    return Results.Problem(
                        title: "Internal Server Error",
                        statusCode: 500,
                        detail: e.Message
                    );
                }
            });
        }

        // =========================================================
        // VALIDASI FORMAT TANGGAL (yyyy-MM-dd)
        // Null / kosong dianggap valid (memakai default).
        // =========================================================
        private static bool TryParseDate(
            string? value,
            out DateTime result)
        {
            result = default;

            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            return DateTime.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out result);
        }
    }
}
