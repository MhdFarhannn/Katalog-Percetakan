using Dapper;
using Katalog.Models;

namespace Katalog.Services
{
    // =========================================================
    // SALES REPORT
    //
    // Agregasi penjualan dari tabel Pesanan (soft delete
    // diperhitungkan: hanya Deleted_At IS NULL) per periode
    // (day / month / year) pada rentang tanggal tertentu.
    //
    // Pendapatan (revenue) dihitung hanya dari pesanan yang
    // pembayaran TERAKHIRNYA berstatus DIBAYAR, sehingga
    // pembatalan / kedaluwarsa tidak ikut menggelembungkan
    // angka penjualan.
    // =========================================================
    public class ReportServices
    {
        private readonly Database db;

        public ReportServices(Database _db)
        {
            db = _db;
        }

        public async Task<SalesReportResponse> GetSalesReportAsync(
            DateTime startAt,
            DateTime endAtInclusive,
            string period)
        {
            using var conn = db.connect();

            // endDate inclusive: filter memakai batas exclusive
            // endDate + 1 hari.
            var endAtExclusive = endAtInclusive.Date.AddDays(1);

            // Grouping & format tanggal ditentukan di MySQL agar
            // bucketing konsisten dengan zona database.
            const string query = @"
                SELECT
                    CASE @Period
                        WHEN 'month' THEN DATE_FORMAT(ps.Created_At, '%Y-%m')
                        WHEN 'year'  THEN DATE_FORMAT(ps.Created_At, '%Y')
                        ELSE DATE_FORMAT(ps.Created_At, '%Y-%m-%d')
                    END AS Period,
                    COUNT(*) AS TotalOrders,
                    COALESCE(SUM(
                        CASE WHEN pay.idPesanan IS NOT NULL
                            THEN 1 ELSE 0 END), 0) AS PaidOrders,
                    COALESCE(SUM(
                        CASE WHEN pay.idPesanan IS NOT NULL
                            THEN ps.total_harga ELSE 0 END), 0) AS Revenue
                FROM Pesanan ps
                LEFT JOIN
                (
                    -- pembayaran TERAKHIR per pesanan yang berstatus DIBAYAR
                    SELECT p.idPesanan
                    FROM payments p
                    INNER JOIN
                    (
                        SELECT idPesanan, MAX(id) AS id
                        FROM payments
                        GROUP BY idPesanan
                    ) terakhir
                        ON terakhir.id = p.id
                    WHERE p.idStatusPayment = @IdStatusPaid
                ) pay
                    ON pay.idPesanan = ps.id
                WHERE ps.Deleted_At IS NULL
                    AND ps.Created_At >= @StartAt
                    AND ps.Created_At < @EndAt
                GROUP BY 1
                ORDER BY 1;";

            var rows = (await conn.QueryAsync<SalesReportRow>(
                query,
                new
                {
                    Period = period,
                    StartAt = startAt.Date,
                    EndAt = endAtExclusive,
                    IdStatusPaid = PaymentStatusMap.Dibayar
                }))
                .ToList();

            var summary = new SalesReportSummary
            {
                TotalOrders = rows.Sum(r => r.TotalOrders),
                PaidOrders = rows.Sum(r => r.PaidOrders),
                TotalRevenue = rows.Sum(r => r.Revenue)
            };

            return new SalesReportResponse
            {
                StartDate = startAt.Date.ToString("yyyy-MM-dd"),
                EndDate = endAtInclusive.Date.ToString("yyyy-MM-dd"),
                Period = period,
                Summary = summary,
                Rows = rows
            };
        }
    }
}
