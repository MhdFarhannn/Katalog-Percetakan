# Payment (Midtrans Snap)

Base path: `/api/v1/payment`

| Method | Endpoint | Auth | Keterangan |
|---|---|---|---|
| POST | `/api/v1/payment/{idPesanan}` | Bearer (pemilik) | Buat transaksi Snap, kembalikan token |
| GET | `/api/v1/payment/pesanan/{idPesanan}` | Bearer (pemilik) | Lihat status pembayaran tersimpan |
| POST | `/api/v1/payment/{idPesanan}/cancel` | Bearer (pemilik) | Batalkan pembayaran yang belum dibayar |
| POST | `/api/v1/payment/midtrans/notification` | Publik | Webhook Midtrans (jangan dipanggil frontend) |

> Environment Midtrans dikontrol backend lewat `appsettings.json`
> (`Midtrans:IsProduction`). Frontend **tidak perlu** tahu. Saat development
> gunakan Midtrans **Sandbox**.

## Model Response — `PaymentResponse`

```json
{
  "idPesanan": 4,
  "midtransOrderId": "PESANAN-4-20260917014109",
  "grossAmount": 100000.00,
  "snapToken": "66e4fa55-fd7c-...",
  "redirectUrl": "https://app.sandbox.midtrans.com/snap/v4/redirection/66e4fa55-...",
  "paymentStatus": "pending"
}
```

| Field | Keterangan |
|---|---|
| `snapToken` | Token untuk dibuka dengan `window.snap.pay()` |
| `redirectUrl` | Alternatif redirect pembayaran |
| `paymentStatus` | Status pembayaran saat ini |

---

## Struktur Tabel `payments`

Status pembayaran **tidak lagi** disimpan sebagai kolom string. Tabel
`payments` menyimpan `idStatusPayment` sebagai **foreign key** ke tabel master
`status_payment`, sehingga status yang dipakai aplikasi selalu salah satu baris
di tabel tersebut.

| Kolom | Tipe | Keterangan |
|---|---|---|
| `id` | int PK | |
| `idPesanan` | int FK → `Pesanan(id)` | `ON DELETE CASCADE` |
| `idStatusPayment` | int FK → `status_payment(id)` | Status pembayaran |
| `midtrans_order_id` | varchar(100) UNIQUE | Order ID yang dikirim ke Midtrans |
| `midtrans_transaction_id` | varchar(100) | Diisi dari notifikasi Midtrans |
| `snap_token` | varchar(255) | Token Snap |
| `payment_type` | varchar(50) | Diisi dari notifikasi Midtrans |
| `gross_amount` | decimal(15,2) | Nominal yang ditagih |
| `transaction_status` | varchar(50) | Status mentah dari Midtrans |
| `transaction_time` | datetime | Diisi dari notifikasi Midtrans |
| `settlement_time` | datetime | Diisi dari notifikasi Midtrans |
| `expiry_time` | datetime | Diisi dari notifikasi Midtrans |
| `created_at` / `updated_at` | timestamp | Otomatis oleh database |

Master status (`status_payment`) dan kode yang dikembalikan API:

| `idStatusPayment` | `status_payment.nama` | `paymentStatus` (API) |
|---|---|---|
| 1 | MENUNGGU PEMBAYARAN | `pending` |
| 2 | DIBAYAR | `paid` |
| 3 | DIBATALKAN | `cancelled` |
| 4 | KEDALUWARSA | `expired` |
| 5 | GAGAL | `failed` |
| 6 | DIKEMBALIKAN | `refunded` |

> `paymentStatus` pada response **tetap memakai kode** (`pending`, `paid`, ...)
> agar kontrak frontend tidak berubah. Label Indonesia hanya ada di tabel
> `status_payment`.
>
> Pemetaan kode → `idStatusPayment` ada di `PaymentStatusMap`
> (`Models/Payment.cs`), sedangkan `PesananResponse.paymentStatus` diturunkan
> dari pembayaran terakhir pesanan tersebut.

---

## POST /api/v1/payment/{idPesanan}

Membuat transaksi Midtrans Snap untuk pesanan milik user.

- Bersifat **idempotent**: jika masih ada pembayaran `pending`
  (`idStatusPayment = 1`) dengan Snap token, server mengembalikan token yang sama
  tanpa membuat transaksi baru.
- Pesanan yang sudah `paid` akan ditolak.
- Pesanan yang sudah dibatalkan (`statusPengerjaan = "Dibatalkan"`) akan ditolak.

**Response 200** — `PaymentResponse`.

**Error `400`**

```json
{ "message": "Pesanan tidak ditemukan" }
{ "message": "Pesanan sudah dibayar" }
{ "message": "Pesanan sudah dibatalkan" }
```

**Error `502`** — gagal ke Midtrans (API error / timeout / jaringan):

```json
{
  "title": "Midtrans Error",
  "status": 502,
  "detail": "Midtrans Snap error (401)."
}
```

```js
const res = await fetch(`${API}/api/v1/payment/${idPesanan}`, {
  method: "POST",
  headers: { Authorization: `Bearer ${token}` },
});
const payment = await res.json();

window.snap.pay(payment.snapToken, {
  onSuccess: () => checkStatus(idPesanan),
  onPending: () => checkStatus(idPesanan),
  onError:   () => alert("Pembayaran gagal"),
  onClose:   () => console.log("Snap ditutup"),
});
```

---

## GET /api/v1/payment/pesanan/{idPesanan}

Mengembalikan pembayaran terbaru untuk pesanan milik user.

**Response 200** — `PaymentResponse` (tanpa `redirectUrl`).

**Error `404`** — `{ "message": "Pembayaran tidak ditemukan" }`.

```js
const res = await fetch(`${API}/api/v1/payment/pesanan/${idPesanan}`, {
  headers: { Authorization: `Bearer ${token}` },
});
const payment = await res.json();
console.log(payment.paymentStatus); // "pending" | "paid" | ...
```

---

## POST /api/v1/payment/{idPesanan}/cancel

Membatalkan pembayaran pesanan milik user, mis. saat pelanggan tidak jadi
checkout atau pembayarannya kedaluwarsa.

- Hanya **pemilik pesanan** (Bearer token, `idUser` diambil dari JWT).
- Bila transaksi sudah terbuat di Midtrans (`snap_token` ada), backend memanggil
  **API Cancel Midtrans** (`POST /v2/{order_id}/cancel`) lebih dulu.
- API Cancel tidak selalu mengembalikan `transaction_status` (mis. `412` karena
  transaksi sudah settlement). Bila kosong, backend mengambil status terakhir
  lewat **API Status Midtrans** dan memakai status itu.
- `payments.idStatusPayment` diperbarui. Bila status akhirnya `cancelled` atau
  `expired`, `Pesanan.idStatusPengerjaan` diubah menjadi `Dibatalkan`.
- Transaksi yang sudah lunas tidak dapat dibatalkan.
- Bersifat **idempotent**: pembayaran yang sudah `cancelled` langsung
  mengembalikan `200` dengan status tersebut tanpa memanggil Midtrans lagi.

**Response 200** — `PaymentResponse`:

```json
{
  "idPesanan": 4,
  "midtransOrderId": "PESANAN-4-20260917014109",
  "grossAmount": 100000.00,
  "snapToken": "66e4fa55-fd7c-...",
  "redirectUrl": null,
  "paymentStatus": "cancelled"
}
```

**Error `400`**

```json
{ "message": "Pembayaran tidak ditemukan" }
{ "message": "Pembayaran sudah dibayar" }
```

**Error `502`** — gagal ke Midtrans (timeout / jaringan / Midtrans tidak
mengembalikan status):

```json
{
  "title": "Midtrans Error",
  "status": 502,
  "detail": "Midtrans timeout."
}
```

```js
const res = await fetch(`${API}/api/v1/payment/${idPesanan}/cancel`, {
  method: "POST",
  headers: { Authorization: `Bearer ${token}` },
});
const payment = await res.json();
console.log(payment.paymentStatus); // "cancelled"
```

> Setelah dibatalkan, pesanan tidak bisa dibayar lagi: `POST
> /api/v1/payment/{idPesanan}` mengembalikan `400`
> `{ "message": "Pesanan sudah dibatalkan" }`.

---

## POST /api/v1/payment/midtrans/notification

Webhook dari server Midtrans. **Jangan dipanggil dari frontend.**

- Publik, tetapi divalidasi dengan `signature_key` (SHA512).
- Idempotent: notifikasi duplikat tidak akan mengubah data dua kali.
- Notifikasi berstatus `cancel` atau `expire` juga menandai pesanan menjadi
  `Dibatalkan` (`idStatusPengerjaan`).
- `401` bila signature tidak valid.
- `200` untuk notifikasi valid / duplikat / order tidak dikenal / jumlah tidak sesuai.

Backend yang mengubah status pembayaran berdasarkan notifikasi ini. Karena itu,
frontend harus melakukan **polling** ke endpoint status sampai `paymentStatus`
menjadi `paid` (lihat [frontend-snap.md](frontend-snap.md)).
