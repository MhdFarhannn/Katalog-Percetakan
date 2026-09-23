# API untuk Frontend Pelanggan

Panduan ringkas: endpoint mana yang dipakai aplikasi **Pelanggan** (Web /
Android), urutannya, dan aturan status pesanan/pembayaran.

Detail body/response tetap ada di dokumen masing-masing:
[authentication.md](authentication.md), [alamat.md](alamat.md),
[pesanan.md](pesanan.md), [payment.md](payment.md),
[frontend-snap.md](frontend-snap.md).

Frontend admin/petugas ada di [admin.md](admin.md).

## 1. Login

| Aksi | Endpoint |
|---|---|
| Registrasi / login Google | `POST /api/v1/auth/google` |
| Login email + password | `POST /api/v1/auth/login` |
| Profil yang sedang login | `GET /api/v1/auth/me` |

Role pelanggan selalu `"Pelanggan"`. Simpan `token`, lalu kirim di setiap
request:

```
Authorization: Bearer <token>
```

## 2. Peta Layar → Endpoint

| Layar Pelanggan | Endpoint | Auth |
|---|---|---|
| Login / daftar | `POST /api/v1/auth/google`, `POST /api/v1/auth/login` | publik |
| Profil | `GET /api/v1/auth/me` | Bearer |
| Katalog produk | `GET /api/v1/products` | publik |
| Katalog layanan | `GET /api/v1/layanan`, `GET /api/v1/layanan/{id}` | Bearer |
| Alamat saya | `GET /api/v1/alamat` | Bearer |
| Tambah alamat | `POST /api/v1/alamat` | Bearer |
| Ubah / hapus alamat | `PATCH` / `DELETE` `/api/v1/alamat/{id}` | Bearer |
| Buat pesanan | `POST /api/v1/pesanan` | Bearer (`multipart/form-data`) |
| Riwayat / daftar pesanan saya | `GET /api/v1/pesanan` | Bearer |
| Detail pesanan | `GET /api/v1/pesanan/{id}` | Bearer |
| Ubah / hapus pesanan (sebelum bayar) | `PUT` / `DELETE` `/api/v1/pesanan/{id}` | Bearer |
| Bayar (ambil Snap token) | `POST /api/v1/payment/{idPesanan}` | Bearer |
| Cek status pembayaran | `GET /api/v1/payment/pesanan/{idPesanan}` | Bearer |
| Batalkan pembayaran | `POST /api/v1/payment/{idPesanan}/cancel` | Bearer |

## 3. Alur Utama

```
Login (JWT)
   ↓
GET /api/v1/alamat          → pilih idAlamat (POST kalau belum ada)
   ↓
GET /api/v1/products        → pilih product (+ ukuran)
GET /api/v1/layanan         → info layanan (opsional)
   ↓
POST /api/v1/pesanan        → dapat idPesanan  (multipart/form-data)
   ↓
POST /api/v1/payment/{id}   → dapat snapToken
   ↓
window.snap.pay(snapToken)  → pelanggan bayar
   ↓
Polling status pembayaran   → sampai paymentStatus = "paid"
```

- Detail langkah Snap (`window.snap.pay`, test card, webhook saat development)
  ada di [frontend-snap.md](frontend-snap.md).
- Pesanan yang belum dibayar masih bisa diubah/dihapus
  (`PUT`/`DELETE /api/v1/pesanan/{id}`). Setelah pembayaran dibuat, keduanya
  ditolak `400`.
- Bila pelanggan tidak jadi membayar: panggil
  `POST /api/v1/payment/{idPesanan}/cancel`. Setelah itu
  `statusPengerjaan` pesanan menjadi `Dibatalkan`.

## 4. Cara Cek Status Pembayaran

Dua cara berikut sama-sama valid, pilih sesuai layar:

| Cara | Endpoint | Catatan |
|---|---|---|
| Halaman pembayaran (polling) | `GET /api/v1/payment/pesanan/{idPesanan}` | Kembalikan `paymentStatus` pembayaran terbaru |
| Halaman riwayat / daftar pesanan | `GET /api/v1/pesanan` atau `GET /api/v1/pesanan/{id}` | Field `paymentStatus` per pesanan |

Keduanya **menarik status terakhir dari Midtrans** untuk pembayaran yang masih
`pending`, jadi tidak perlu menunggu webhook. Bila Midtrans tidak dapat
dihubungi, response tetap `200` berisi status yang tersimpan.

```js
// polling sederhana setelah window.snap.pay
for (let i = 0; i < 15; i++) {
  const res = await fetch(`${API}/api/v1/payment/pesanan/${idPesanan}`, {
    headers: { Authorization: `Bearer ${token}` },
  });

  if (res.ok) {
    const { paymentStatus } = await res.json();
    if (paymentStatus === "paid") return tampilkan("Pembayaran berhasil!");
    if (["failed", "expired", "cancelled"].includes(paymentStatus)) {
      return tampilkan(`Pembayaran ${paymentStatus}`);
    }
  }

  await new Promise((r) => setTimeout(r, 2000));
}
```

## 5. Field Status pada Pesanan

`GET /api/v1/pesanan` dan `GET /api/v1/pesanan/{id}` mengembalikan dua status
berbeda — jangan dicampur:

| Field | Nilai | Arti |
|---|---|---|
| `paymentStatus` | `unpaid` | Belum ada transaksi pembayaran (tombol **Bayar** ditampilkan) |
| | `pending` | Pembayaran dibuat, menunggu pelanggan membayar |
| | `paid` | Sudah lunas (settlement/capture) |
| | `cancelled` / `expired` / `failed` / `refunded` | Pembayaran batal / kedaluwarsa / gagal / dikembalikan |
| `statusPengerjaan` | `Sedang Berlangsung` | Pesanan diproses |
| | `Dibatalkan` | Pesanan dibatalkan (pembayaran dibatalkan atau kedaluwarsa) |

Aturan untuk frontend:

- **Jangan** menandai pesanan lunas dari callback `onSuccess` Snap. Status resmi
  hanya dari Midtrans (lihat [payment.md](payment.md)).
- Tampilkan tombol **Bayar** hanya bila `paymentStatus` bernilai `unpaid` dan
  `statusPengerjaan` **bukan** `Dibatalkan`.
- Bila `statusPengerjaan = "Dibatalkan"`, `POST /api/v1/payment/{idPesanan}`
  menjawab `400` `{ "message": "Pesanan sudah dibatalkan" }` — sembunyikan
  tombol bayar dan tampilkan tombol pesan ulang.
- Pesanan yang sudah punya baris pembayaran (walau sudah dibatalkan) **tidak
  bisa** diubah/dihapus lagi.

## 6. Yang Bukan untuk Pelanggan

| Endpoint | Kenapa |
|---|---|
| `GET /api/v1/pesanan/all` | Khusus `Admin`/`Petugas` → `403` untuk pelanggan |
| `GET /api/v1/alamat/all`, `GET /api/v1/alamat/user/{idUser}` | Dipakai layar admin |
| `POST /api/v1/products`, `POST`/`PATCH`/`DELETE /api/v1/layanan` | Aksi katalog, bukan aksi pelanggan |
| `POST /api/v1/payment/midtrans/notification` | Dipanggil server Midtrans, **jangan** dari frontend |

Alamat selalu terikat ke pemiliknya: `PATCH`/`DELETE /api/v1/alamat/{id}`
hanya berhasil untuk alamat milik user yang login.
