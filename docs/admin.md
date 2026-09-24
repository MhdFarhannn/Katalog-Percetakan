# API untuk Frontend Admin & Petugas

Panduan ringkas: endpoint mana yang dipakai layar **Admin/Petugas**, dan mana
yang **bukan** untuk role ini. Detail body/response tetap ada di dokumen
masing-masing: [master-data.md](master-data.md), [alamat.md](alamat.md),
[pesanan.md](pesanan.md), [payment.md](payment.md).

Frontend pelanggan ada di [pelanggan.md](pelanggan.md).

## 1. Role & Login

| Role | Nilai `role` di JWT | Cara dapat akun |
|---|---|---|
| `Admin` | `"Admin"` | `POST /api/v1/auth/register-admin` (hanya bisa sekali) |
| `Petugas` | `"Petugas"` | belum ada endpoint pembuatan petugas (lihat catatan di bawah) |

Semua request butuh header berikut, kecuali endpoint yang ditandai **publik**:

```
Authorization: Bearer <token>
```

Token didapat dari `POST /api/v1/auth/login`. Role juga bisa dicek lewat
`GET /api/v1/auth/me` (lihat [authentication.md](authentication.md)).

> Sembunyikan menu berdasarkan `role` dari JWT, tetapi tetap tangani `403`
> dari server — policy di server adalah penentu akhir.

## 2. Peta Layar → Endpoint

| Layar Admin/Petugas | Endpoint | Auth |
|---|---|---|
| Login | `POST /api/v1/auth/login` | publik |
| Profil admin yang login | `GET /api/v1/auth/me` | Bearer |
| Daftar kategori product | `GET /api/v1/kategory-product` | publik |
| Kelola kategori product | `POST` / `PATCH` / `DELETE` `/api/v1/kategory-product[/{id}]` | publik |
| Daftar status product | `GET /api/v1/status-product` | publik |
| Kelola status product | `POST` / `PATCH` / `DELETE` `/api/v1/status-product[/{id}]` | publik |
| Daftar status pengerjaan | `GET /api/v1/status-pengerjaan` | publik |
| Kelola status pengerjaan | `POST` / `PATCH` / `DELETE` `/api/v1/status-pengerjaan[/{id}]` | publik |
| Daftar produk | `GET /api/v1/products` | publik |
| Tambah produk | `POST /api/v1/products` | `Admin` |
| Ubah / hapus produk | `PATCH` / `DELETE` `/api/v1/products/{id}` | publik |
| Daftar & detail layanan | `GET /api/v1/layanan`, `GET /api/v1/layanan/{id}` | Bearer |
| Tambah/ubah/hapus layanan | `POST` / `PATCH` / `DELETE` `/api/v1/layanan[/{id}]` | Bearer |
| Daftar pesanan masuk | `GET /api/v1/pesanan/all` | `Admin` / `Petugas` |
| Riwayat pesanan (filter tanggal/status) | `GET /api/v1/pesanan/history` | `Admin` / `Petugas` / `Pelanggan` |
| Detail pesanan | `GET /api/v1/pesanan/{id}` | `Admin` / `Petugas` |
| Ubah status pengerjaan pesanan | `PUT /api/v1/pesanan/{id}/status` | `Admin` |
| Laporan penjualan | `GET /api/v1/reports/sales` | `Admin` / `Petugas` |
| Daftar semua alamat | `GET /api/v1/alamat/all` | Bearer |
| Alamat milik satu user | `GET /api/v1/alamat/user/{idUser}` | Bearer |

## 3. Alur Kerja Admin/Petugas

```
Login                                   → POST /api/v1/auth/login
   ↓
Siapkan katalog (sekali di awal)
   → kategori product, status product, status pengerjaan
   → POST /api/v1/products            (role Admin, multipart/form-data)
   → POST /api/v1/layanan             (multipart/form-data)
   ↓
Pantau pesanan masuk
   → GET /api/v1/pesanan/all          (urut id terbaru di depan)
   ↓
Buka detail pesanan
   → GET /api/v1/pesanan/{id}         (details[], alamat, paymentStatus)
   ↓
Ubah status pengerjaan (Admin)
   → PUT /api/v1/pesanan/{id}/status  ({ "statusPengerjaan": "Selesai" })
   ↓
Lihat laporan penjualan
   → GET /api/v1/reports/sales        (?startDate=&endDate=&period=day)
```

Contoh memuat daftar pesanan untuk halaman admin:

```js
const res = await fetch(`${API}/api/v1/pesanan/all`, {
  headers: { Authorization: `Bearer ${token}` },
});
const pesanan = await res.json();

// setiap item berisi:
//   statusPengerjaan : "Sedang Berlangsung" | "Dibatalkan"
//   paymentStatus    : "unpaid" | "pending" | "paid" | "expired" | ...
//   totalHarga, alamat, details[]
```

`GET /api/v1/pesanan/all` dan `GET /api/v1/pesanan/{id}` juga menyegarkan
status pembayaran yang masih `pending` ke Midtrans sebelum response dikirim,
jadi `paymentStatus` di halaman admin selalu status terakhir.

## 4. Yang TIDAK Bisa Dilakukan Admin

Ini penting supaya tidak salah menampilkan tombol di frontend admin:

| Aksi | Kenapa tidak bisa |
|---|---|
| Ubah / hapus pesanan pelanggan (`PUT`/`DELETE /api/v1/pesanan/{id}`) | Server hanya mengizinkan **pemilik** pesanan (dari JWT) |
| Buat pesanan (`POST /api/v1/pesanan`) atas nama pelanggan | Pesanan selalu dibuat untuk `idUser` di token |
| Buat pembayaran untuk pesanan pelanggan (`POST /api/v1/payment/{idPesanan}`) | Pesanan harus milik user di token |
| Lihat pembayaran lewat `GET /api/v1/payment/pesanan/{idPesanan}` | Hanya pemilik pesanan; admin → `404` |
| Batalkan pembayaran pelanggan (`POST /api/v1/payment/{idPesanan}/cancel`) | Hanya pemilik pesanan |
| Ubah / hapus alamat pelanggan (`PATCH`/`DELETE /api/v1/alamat/{id}`) | Server selalu memfilter `idUser` dari token — admin hanya bisa mengubah alamatnya sendiri |
| Ubah status pengerjaan pesanan (`PUT /api/v1/pesanan/{id}/status`) | Bukan untuk role `Petugas` — endpoint ini **khusus `Admin`** (`403` untuk petugas) |

Untuk status pembayaran di layar admin, pakai field `paymentStatus` pada
`GET /api/v1/pesanan/all` atau `GET /api/v1/pesanan/{id}` — bukan endpoint
`/api/v1/payment/...` yang khusus pemilik.

## 5. Catatan

- **Ubah status pengerjaan: `PUT /api/v1/pesanan/{id}/status` (khusus Admin).**
  Terima `application/json` berisi `idStatusPengerjaan` (int) **atau**
  `statusPengerjaan` (nama, mis. `"Selesai"`). Selain itu status masih berubah
  otomatis menjadi `Dibatalkan` ketika pembayaran dibatalkan / kedaluwarsa.
  Detail request/response: [pesanan.md](pesanan.md#put-apiv1pesananidstatus-admin)
  dan [laporan.md](laporan.md).
- **Riwayat & laporan penjualan:**
  `GET /api/v1/pesanan/history` (Order History, paginasi + filter tanggal)
  dan `GET /api/v1/reports/sales` (Sales Report, agregasi per `day`/`month`/`year`)
  — lihat [laporan.md](laporan.md).
- **Akun `Petugas` belum bisa dibuat lewat API.** `Controller/PetugasController.cs`
  masih kosong, jadi pembuatan petugas harus lewat database dulu.
- **Otorisasi sebagian master data masih terbuka** (`kategory-product`,
  `status-product`, `status-pengerjaan`, serta `PATCH`/`DELETE /api/v1/products`
  belum dibatasi role). Jangan mengandalkan role untuk menyembunyikan aksi
  tulis master data — lihat catatan di [master-data.md](master-data.md).
- Produk dibuat dengan **`multipart/form-data`** (field `Image` file), sedangkan
  `PATCH /api/v1/products/{id}` memakai **JSON**. Layanan selalu multipart.
- Pesanan (`POST`/`PUT /api/v1/pesanan`) wajib `multipart/form-data`, bukan JSON.
