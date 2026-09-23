# Frontend — Integrasi Midtrans Snap

Panduan ini menjelaskan cara menghubungkan frontend dengan backend untuk
pembayaran Midtrans **Sandbox**.

## Aturan Penting

- **JANGAN** menaruh Midtrans **Server Key** di frontend. Server Key hanya
  boleh ada di backend (`appsettings.json`).
- Yang dipakai frontend hanya **Client Key** (Sandbox).
- **JANGAN** menandai pesanan `paid` hanya karena callback `onSuccess`.
  Status resmi ditentukan oleh Midtrans (webhook + sinkronisasi saat polling).

## 1. Tambahkan Snap Script

Tambahkan di `index.html` (atau template utama):

```html
<!-- Sandbox -->
<script
  src="https://app.sandbox.midtrans.com/snap/snap.js"
  data-client-key="YOUR_SANDBOX_CLIENT_KEY">
</script>
```

Untuk produksi nanti:

```html
<script
  src="https://app.midtrans.com/snap/snap.js"
  data-client-key="YOUR_PRODUCTION_CLIENT_KEY">
</script>
```

Client Key Sandbox didapat dari Midtrans Dashboard →
**Settings → Access Keys**.

## 2. Alur Pembayaran

```
Login (JWT)
   ↓
GET /api/v1/alamat            → pilih idAlamat
   ↓
GET /api/v1/products          → pilih product & ukuran
   ↓
POST /api/v1/pesanan          → dapat idPesanan
   ↓
POST /api/v1/payment/{id}     → dapat snapToken
   ↓
window.snap.pay(snapToken)    → pelanggan bayar
   ↓
Backend menerima webhook Midtrans
   ↓
Polling GET /api/v1/payment/pesanan/{id} → paymentStatus = "paid"
```

Jika pelanggan tidak jadi membayar, panggil
`POST /api/v1/payment/{idPesanan}/cancel` (lihat bagian
[6. Batalkan Pembayaran](#6-batalkan-pembayaran-opsional)).

## 3. Contoh Lengkap (Vanilla JS)

```js
const API = "http://localhost:5283";

function getToken() {
  return localStorage.getItem("token");
}

function authHeaders(json = true) {
  const h = { Authorization: `Bearer ${getToken()}` };
  if (json) h["Content-Type"] = "application/json";
  return h;
}

// Endpoint POST/PUT /api/v1/pesanan WAJIB multipart/form-data:
// JANGAN set Content-Type di header — FormData membuat boundary otomatis.
function authHeadersForm() {
  return { Authorization: `Bearer ${getToken()}` };
}

// 1) Login
async function login(email, password) {
  const res = await fetch(`${API}/api/v1/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  });
  if (!res.ok) throw new Error("Login gagal");
  const data = await res.json();
  localStorage.setItem("token", data.token);
  return data;
}

// 2) Buat pesanan — multipart/form-data (BUKAN JSON)
async function createPesanan(idAlamat, items) {
  const form = new FormData();
  form.append("idAlamat", String(idAlamat));

  items.forEach((item, i) => {
    form.append(`items[${i}].idProduct`, String(item.idProduct));
    form.append(`items[${i}].qty`, String(item.qty));
    if (item.idUkuranProduk != null)
      form.append(`items[${i}].idUkuranProduk`, String(item.idUkuranProduk));
    if (item.ukuranCustom)
      form.append(`items[${i}].ukuranCustom`, item.ukuranCustom);
    if (item.notes) form.append(`items[${i}].notes`, item.notes);
    if (item.desain) form.append(`items[${i}].desain`, item.desain); // file, bukan Base64
    if (item.desainText)
      form.append(`items[${i}].desainText`, item.desainText);
  });

  const res = await fetch(`${API}/api/v1/pesanan`, {
    method: "POST",
    headers: authHeadersForm(), // TANPA Content-Type — boundary dibuat otomatis
    body: form, // BUKAN JSON.stringify
  });
  if (!res.ok) throw new Error((await res.json()).message);
  return res.json(); // { id, totalHarga, ... }
}

// 3) Buat pembayaran + buka Snap
async function bayar(idPesanan) {
  const res = await fetch(`${API}/api/v1/payment/${idPesanan}`, {
    method: "POST",
    headers: authHeaders(false),
  });
  if (!res.ok) throw new Error((await res.json()).message);

  const { snapToken } = await res.json();

  window.snap.pay(snapToken, {
    onSuccess: () => tungguPembayaran(idPesanan),
    onPending: () => tungguPembayaran(idPesanan),
    onError: () => tampilkanStatus("Pembayaran gagal"),
    onClose: () => tampilkanStatus("Snap ditutup sebelum selesai"),
  });
}

// 4) Polling sampai webhook mengubah status
async function tungguPembayaran(idPesanan) {
  tampilkanStatus("Menunggu konfirmasi pembayaran...");

  for (let i = 0; i < 15; i++) {
    const res = await fetch(`${API}/api/v1/payment/pesanan/${idPesanan}`, {
      headers: authHeaders(false),
    });

    if (res.ok) {
      const { paymentStatus } = await res.json();

      if (paymentStatus === "paid") {
        tampilkanStatus("Pembayaran berhasil!");
        return;
      }
      if (["failed", "expired", "cancelled"].includes(paymentStatus)) {
        tampilkanStatus(`Pembayaran ${paymentStatus}`);
        return;
      }
    }

    await new Promise((r) => setTimeout(r, 2000));
  }

  tampilkanStatus("Pembayaran masih diproses");
}

function tampilkanStatus(msg) {
  const el = document.getElementById("status");
  if (el) el.textContent = msg;
}

// Pemakaian
// await login("email@contoh.com", "password");
// const pesanan = await createPesanan(5, [{ idProduct: 5, qty: 1 }]);
// await bayar(pesanan.id);
```

## 4. Test Card Sandbox

Di popup Snap Sandbox, pilih metode pembayaran yang tersedia (VA, GoPay,
kartu kredit). Untuk kartu kredit, kartu sukses yang umum didokumentasikan
Midtrans adalah:

```
Nomor : 4811 1111 1111 1114
CVV   : 123
Expiry: tanggal apa pun di masa depan
```

> Daftar terbaru bisa berbeda — cek dokumentasi Midtrans Sandbox.

## 5. Webhook Saat Development

Midtrans **tidak bisa** memanggil `localhost`. Untuk menguji pembaruan status
secara end-to-end:

1. Jalankan tunnel, misalnya:
   ```bash
   ngrok http 5283
   ```
2. Set URL notifikasi di Midtrans Dashboard →
   **Settings → Configuration → Payment Notification URL**:
   ```
   https://<url-tunnel-anda>/api/v1/payment/midtrans/notification
   ```
3. Lakukan pembayaran Sandbox, lalu cek log API:
   ```
   POST /api/v1/payment/midtrans/notification 200
   ```
4. `paymentStatus` akan berubah menjadi `paid` dan polling frontend berhenti.

Tanpa tunnel, `paymentStatus` tetap ter-update saat frontend polling, karena
backend menarik status terakhir dari Midtrans di endpoint
`GET /api/v1/payment/pesanan/{idPesanan}`. Webhook tetap disarankan agar status
berubah tanpa menunggu polling (atau gunakan **Resend notification** dari
dashboard Midtrans).

## 6. Batalkan Pembayaran (Opsional)

Panggil endpoint cancel bila pelanggan **tidak jadi checkout** (mis. menutup
Snap tanpa membayar lalu membatalkan pesanan). Backend yang memanggil API
Cancel Midtrans, jadi frontend tidak perlu tahu `order_id`.

```js
async function batalkanPembayaran(idPesanan) {
  const res = await fetch(`${API}/api/v1/payment/${idPesanan}/cancel`, {
    method: "POST",
    headers: authHeaders(false),
  });

  const body = await res.json();

  // 400: "Pembayaran sudah dibayar" / "Pembayaran tidak ditemukan"
  if (!res.ok) throw new Error(body.message);

  // body.paymentStatus === "cancelled"
  tampilkanStatus("Pembayaran dibatalkan");
  return body;
}
```

Setelah dibatalkan, `POST /api/v1/payment/{idPesanan}` akan menolak dengan
`400` `{ "message": "Pesanan sudah dibatalkan" }` — jadi jangan tampilkan lagi
tombol bayar untuk pesanan tersebut.

## 7. CORS

Backend mengizinkan origin berikut (policy `AllowWebFrontend`):

```
http://localhost:3000
http://localhost:5174
http://localhost:4200
http://127.0.0.1:5174
https://yourdomain.com
```

Jika frontend berjalan di origin lain (mis. `http://localhost:5173`), tambahkan
origin tersebut pada policy `AllowWebFrontend` di `Program.cs`.
