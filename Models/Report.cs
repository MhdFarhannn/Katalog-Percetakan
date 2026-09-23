namespace Katalog.Models
{
    // =========================================================
    // GET /api/v1/reports/sales (Sales Report)
    //
    // Query parameter diikat dari URL:
    //   startDate  yyyy-MM-dd (inclusive)
    //   endDate    yyyy-MM-dd (inclusive)
    //   period     day | month | year (default day)
    // =========================================================
    public class SalesReportQuery
    {
        public string? StartDate { get; set; }

        public string? EndDate { get; set; }

        public string Period { get; set; } = "day";
    }

    public class SalesReportResponse
    {
        public string StartDate { get; set; } = string.Empty;

        public string EndDate { get; set; } = string.Empty;

        public string Period { get; set; } = "day";

        public SalesReportSummary Summary { get; set; } = new();

        public List<SalesReportRow> Rows { get; set; } = new();
    }

    public class SalesReportSummary
    {
        public int TotalOrders { get; set; }

        public int PaidOrders { get; set; }

        public decimal TotalRevenue { get; set; }
    }

    public class SalesReportRow
    {
        // Bucket waktu sesuai period:
        // day   -> "2026-09-01"
        // month -> "2026-09"
        // year  -> "2026"
        public string Period { get; set; } = string.Empty;

        public int TotalOrders { get; set; }

        public int PaidOrders { get; set; }

        public decimal Revenue { get; set; }
    }
}
