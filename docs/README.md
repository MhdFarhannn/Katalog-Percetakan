# Katalog Percetakan — API Documentation

Dokumentasi ini ditujukan untuk **frontend** (Web / Android) yang akan
mengonsumsi API Katalog Percetakan.

## Daftar Isi

| Dokumen | Isi |
|---|---|
| [authentication.md](authentication.md) | Login, register admin, login Google, profil |
| [master-data.md](master-data.md) | Kategori, status product, status pengerjaan, product, layanan |
| [alamat.md](alamat.md) | CRUD alamat pelanggan |
| [pesanan.md](pesanan.md) | CRUD pesanan (order) + detail item |
| [payment.md](payment.md) | Pembuatan, pembatalan, dan status pembayaran Midtrans Snap |
| [frontend-snap.md](frontend-snap.md) | Integrasi Midtrans Snap di frontend |

## Base URL

| Environment | Base URL |
|---|---|
| Local development | `http://localhost:5283` |
| Swagger UI | `http://localhost:5283/swagger` |

Semua endpoint memakai prefix `/api/v1`.

## Autentikasi

API memakai **JWT Bearer Token**.

Kirim token pada header:

```
Authorization: Bearer <token>
```

- Token didapat dari endpoint login (`authentication.md`).
- Masa berlaku token: **7 hari**.
- Login juga mengembalikan `refreshToken`, namun **belum ada endpoint refresh**
  saat ini. Jika token kedaluwarsa, lakukan login ulang.
- Role yang tersedia: `Admin`, `Petugas`, `Pelanggan`.

## Format Request

- Request body JSON memakai **camelCase** (binding juga menerima PascalCase).
- Endpoint upload gambar memakai **multipart/form-data**.
- **POST** dan **PUT** `/api/v1/pesanan` (order) juga **wajib** memakai
  **`multipart/form-data`** — JANGAN kirim JSON ke endpoint ini. Kirim setiap
  parameter sebagai form field terpisah (lihat [pesanan.md](pesanan.md)).
  File desain dikirim sebagai multipart file field, bukan Base64.

```http
Content-Type: application/json
```

```http
Content-Type: multipart/form-data
```

## Format Response & Error

### Sukses

```json
{
  "id": 1,
  "nama": "Contoh"
}
```

### Validasi / Bad Request — `400`

```json
{
  "message": "Jumlah minimal 1"
}
```

### Not Found — `404`

```json
{
  "message": "Pesanan tidak ditemukan"
}
```

### Unauthorized — `401`

Token tidak ada / tidak valid. Body bisa kosong, atau:

```json
{
  "message": "Signature notifikasi tidak valid"
}
```

### Forbidden — `403`

Role tidak memiliki akses (mis. Pelanggan membuka endpoint Admin).

### Internal Server Error — `500`

Format `application/problem+json`:

```json
{
  "title": "Internal Server Error",
  "status": 500,
  "detail": "..."
}
```

### Midtrans Error — `502`

```json
{
  "title": "Midtrans Error",
  "status": 502,
  "detail": "Midtrans Snap error (401)."
}
```

## Status Pembayaran

Nilai `paymentStatus` yang mungkin:

| Status | Arti |
|---|---|
| `unpaid` | Belum ada transaksi pembayaran |
| `pending` | Pembayaran dibuat, menunggu pelanggan |
| `paid` | Pembayaran berhasil (settlement/capture) |
| `failed` | Ditolak / chargeback |
| `expired` | Kedaluwarsa |
| `cancelled` | Dibatalkan |
| `refunded` | Dikembalikan |

> Status di atas disimpan pada tabel `payments` sebagai `idStatusPayment`, yaitu
> foreign key ke tabel master `status_payment`. API tetap mengembalikan kode
> (`pending`, `paid`, ...) — rincian kolom dan pemetaannya ada di
> [payment.md](payment.md#struktur-tabel-payments).
>
> Pembatalan pembayaran oleh pelanggan memakai
> `POST /api/v1/payment/{idPesanan}/cancel`.

> **Penting:** frontend **tidak boleh** menandai pesanan sebagai `paid`
> berdasarkan callback Snap. Status pembayaran yang sah hanya yang berasal dari
> Midtrans, yaitu notifikasi/webhook ke backend atau hasil sinkronisasi yang
> dilakukan backend saat `GET /api/v1/payment/pesanan/{idPesanan}` di-polling.

## CORS

Kebijakan `AllowWebFrontend` mengizinkan origin berikut:

```
http://localhost:3000
http://localhost:5174
http://localhost:4200
http://127.0.0.1:5174
https://yourdomain.com
```

Jika frontend berjalan di origin lain (mis. Vite default `http://localhost:5173`),
origin tersebut harus ditambahkan di `Program.cs` pada policy `AllowWebFrontend`.

## Keamanan

- **Jangan pernah** menaruh `ServerKey` Midtrans di frontend.
- Hanya `ClientKey` Sandbox yang boleh dipakai di frontend.
- Selalu pakai HTTPS untuk produksi.
