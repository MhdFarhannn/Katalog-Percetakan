# Payment (Midtrans Snap)

Base path: `/api/v1/payment`

| Method | Endpoint | Auth | Keterangan |
|---|---|---|---|
| POST | `/api/v1/payment/{idPesanan}` | Bearer (pemilik) | Buat transaksi Snap, kembalikan token |
| GET | `/api/v1/payment/pesanan/{idPesanan}` | Bearer (pemilik) | Lihat status pembayaran tersimpan |
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

## POST /api/v1/payment/{idPesanan}

Membuat transaksi Midtrans Snap untuk pesanan milik user.

- Bersifat **idempotent**: jika masih ada pembayaran `pending` dengan Snap token,
  server mengembalikan token yang sama tanpa membuat transaksi baru.
- Pesanan yang sudah `paid` akan ditolak.

**Response 200** — `PaymentResponse`.

**Error `400`**

```json
{ "message": "Pesanan tidak ditemukan" }
{ "message": "Pesanan sudah dibayar" }
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

## POST /api/v1/payment/midtrans/notification

Webhook dari server Midtrans. **Jangan dipanggil dari frontend.**

- Publik, tetapi divalidasi dengan `signature_key` (SHA512).
- Idempotent: notifikasi duplikat tidak akan mengubah data dua kali.
- `401` bila signature tidak valid.
- `200` untuk notifikasi valid / duplikat / order tidak dikenal / jumlah tidak sesuai.

Backend yang mengubah status pembayaran berdasarkan notifikasi ini. Karena itu,
frontend harus melakukan **polling** ke endpoint status sampai `paymentStatus`
menjadi `paid` (lihat [frontend-snap.md](frontend-snap.md)).
