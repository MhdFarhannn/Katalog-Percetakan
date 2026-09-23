# Order History & Sales Report

Dokumen ini menjelaskan dua endpoint pelaporan:

1. **Order History** — daftar pesanan berdasarkan parameter filter.
2. **Sales Report** — agregasi/summary penjualan per periode.

Dokumen terkait: [pesanan.md](pesanan.md) (CRUD pesanan),
[schema.md](schema.md) (skema DB, audit & soft delete),
[admin.md](admin.md) (panduan role).

---

# Order History

Daftar riwayat pesanan berdasarkan parameter (rentang tanggal, status,
user, paginasi). Baris yang sudah **soft delete** (`Deleted_At IS NOT NULL`)
tidak pernah ikut.

## Resource URL & Method

| | |
|---|---|
| **Resource URL** | `/api/v1/pesanan/history` |
| **HTTP Method** | `GET` |
| **Content-Type** | — (tidak ada body) |
| **Auth** | Bearer token — `Admin` / `Petugas` / `Pelanggan` |

`idUser` pelanggan **selalu** diambil dari JWT (parameter `idUser` diabaikan
untuk `Pelanggan`). `Admin`/`Petugas` bisa memakai `idUser` untuk melihat
riwayat pelanggan tertentu.

## Request — Query Parameter

| Parameter | Tipe | Wajib | Default | Keterangan |
|---|---|---|---|---|
| `startDate` | `yyyy-MM-dd` | Tidak | — | Rentang awal, inclusive |
| `endDate` | `yyyy-MM-dd` | Tidak | — | Rentang akhir, inclusive (sepanjang hari) |
| `idStatusPengerjaan` | int | Tidak | — | Filter status pengerjaan |
| `idUser` | int | Tidak | semua user (staff) | Khusus `Admin`/`Petugas` |
| `page` | int | Tidak | `1` | Di-clamp `>= 1` |
| `pageSize` | int | Tidak | `10` | Di-clamp `1..100` |

```http
GET /api/v1/pesanan/history?startDate=2026-09-01&endDate=2026-09-30&idStatusPengerjaan=3&page=1&pageSize=10 HTTP/1.1
Authorization: Bearer <token>
```

```js
const params = new URLSearchParams({
  startDate: "2026-09-01",
  endDate: "2026-09-30",
  idStatusPengerjaan: "3",
  page: "1",
  pageSize: "10",
});

const res = await fetch(`${API}/api/v1/pesanan/history?${params}`, {
  headers: { Authorization: `Bearer ${token}` },
});
const { items, total, page, pageSize } = await res.json();
```

## Response 200

```json
{
  "items": [
    {
      "id": 12,
      "idUser": 7,
      "namaUser": "Habib Herdiansyah",
      "idAlamat": 5,
      "alamat": "Koto Tuo, Payakumbuh, West Sumatra, 26218, Indonesia",
      "idStatusPengerjaan": 3,
      "statusPengerjaan": "Selesai",
      "totalHarga": 150000.00,
      "paymentStatus": "paid",
      "createdAt": "2026-09-17T08:18:46+07:00",
      "updatedAt": "2026-09-23T10:12:05+07:00",
      "details": [
        {
          "id": 31,
          "idPesanan": 12,
          "idProduct": 5,
          "namaProduct": "Bodi Bakar",
          "qty": 1,
          "hargaSatuan": 150000.00,
          "desainFilePath": null,
          "desainText": null
        }
      ]
    }
  ],
  "total": 57,
  "page": 1,
  "pageSize": 10
}
```

| Field | Keterangan |
|---|---|
| `items` | `PesananResponse[]` urut `id` terbaru, termasuk `details[]` |
| `total` | Jumlah total baris yang cocok dengan filter (untuk paginasi) |
| `page` / `pageSize` | Halaman aktif & ukuran halaman **setelah** di-clamp |

Sama seperti endpoint pesanan lain, pembayaran yang masih `pending`
disinkronkan ke Midtrans sebelum response dikirim, jadi `paymentStatus`
adalah status terakhir.

## Error

| Status | Body |
|---|---|
| `400` | `{ "message": "Format startDate harus yyyy-MM-dd" }` |
| `400` | `{ "message": "Format endDate harus yyyy-MM-dd" }` |
| `401` | Tanpa / token tidak valid |
| `500` | `application/problem+json` |

---

# Sales Report

Summary & agregasi penjualan pada rentang tanggal, dikelompokkan per
periode (`day` / `month` / `year`).

Definisi angka:

- **`totalOrders`** — semua pesanan **aktif** (`Deleted_At IS NULL`) pada
  rentang tanggal, terlepas dari status bayar.
- **`paidOrders` / `revenue`** — hanya pesanan yang pembayaran
  **terakhirnya** berstatus `DIBAYAR` (`idStatusPayment = 2`). Pembatalan /
  kedaluwarsa tidak dihitung sebagai pendapatan.

## Resource URL & Method

| | |
|---|---|
| **Resource URL** | `/api/v1/reports/sales` |
| **HTTP Method** | `GET` |
| **Content-Type** | — (tidak ada body) |
| **Auth** | Bearer token — `Admin` / `Petugas` (`403` untuk `Pelanggan`) |

## Request — Query Parameter

| Parameter | Tipe | Wajib | Default | Keterangan |
|---|---|---|---|---|
| `startDate` | `yyyy-MM-dd` | Tidak | `endDate - 29 hari` | Rentang awal, inclusive |
| `endDate` | `yyyy-MM-dd` | Tidak | hari ini | Rentang akhir, inclusive |
| `period` | `day` \| `month` \| `year` | Tidak | `day` | Bucketing baris |

```http
GET /api/v1/reports/sales?startDate=2026-09-01&endDate=2026-09-30&period=day HTTP/1.1
Authorization: Bearer <token-admin>
```

```js
const params = new URLSearchParams({
  startDate: "2026-09-01",
  endDate: "2026-09-30",
  period: "day",
});

const res = await fetch(`${API}/api/v1/reports/sales?${params}`, {
  headers: { Authorization: `Bearer ${token}` },
});
const report = await res.json();
```

Format `period` pada setiap baris `rows[].period`:

| `period` | Format bucket | Contoh |
|---|---|---|
| `day` | `yyyy-MM-dd` | `2026-09-17` |
| `month` | `yyyy-MM` | `2026-09` |
| `year` | `yyyy` | `2026` |

## Response 200

```json
{
  "startDate": "2026-09-01",
  "endDate": "2026-09-30",
  "period": "day",
  "summary": {
    "totalOrders": 12,
    "paidOrders": 9,
    "totalRevenue": 1350000.00
  },
  "rows": [
    {
      "period": "2026-09-01",
      "totalOrders": 2,
      "paidOrders": 2,
      "revenue": 250000.00
    },
    {
      "period": "2026-09-17",
      "totalOrders": 3,
      "paidOrders": 2,
      "revenue": 400000.00
    }
  ]
}
```

| Field | Keterangan |
|---|---|
| `startDate` / `endDate` | Rentang efektif setelah default diterapkan |
| `period` | Bucketing yang dipakai |
| `summary.totalOrders` | Total pesanan aktif pada rentang |
| `summary.paidOrders` | Pesanan aktif ber-status bayar terakhir `DIBAYAR` |
| `summary.totalRevenue` | Σ `total_harga` pesanan yang ber-status bayar |
| `rows` | Agregasi per bucket — hanya periode yang punya pesanan |

> `summary` dihitung dari Σ seluruh `rows`, sehingga selalu konsisten
> dengan detail di bawahnya. Rentang tanpa pesanan tidak menghasilkan
> baris (bukan nol-padding).

## Error

| Status | Body |
|---|---|
| `400` | `{ "message": "Period harus salah satu dari: day, month, year" }` |
| `400` | `{ "message": "Format startDate harus yyyy-MM-dd" }` |
| `400` | `{ "message": "Format endDate harus yyyy-MM-dd" }` |
| `400` | `{ "message": "startDate tidak boleh melebihi endDate" }` |
| `401` | Tanpa / token tidak valid |
| `403` | Role `Pelanggan` |
| `500` | `application/problem+json` |
