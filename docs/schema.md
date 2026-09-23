# Database Schema, Audit & Soft Delete

Dokumen ini menjelaskan skema database Katalog Percetakan: daftar tabel,
kolom audit, strategi **soft delete**, dan contoh payload API untuk
membaca daftar pesanan serta mengubah status pengerjaan dari sisi admin.

- DDL lengkap (fresh install): [`Schema/setup.sql`](../Schema/setup.sql)
- Migrasi database yang sudah berjalan:
  [`Schema/migration_soft_delete_audit.sql`](../Schema/migration_soft_delete_audit.sql)
- Database: **MySQL/MariaDB**, engine `InnoDB`, charset `utf8mb4`
  (`utf8mb4_unicode_ci`)

## 1. Ringkasan Tabel

Nama tabel mengikuti kode aplikasi. Padanan dengan istilah umum:

| Tabel aktual | Istilah umum | Isi | Soft delete |
|---|---|---|---|
| `User` | `users` | Akun (Admin / Petugas / Pelanggan) | — (pakai `Is_Active`) |
| `Roles` | `roles` | Master role (1 Admin, 2 Petugas, 3 Pelanggan) | — |
| `Alamat` | `addresses` | Alamat pelanggan | ✔ `deleted_at` |
| `Pesanan` | `orders` | Header pesanan | ✔ `Deleted_At` |
| `Pesanan_Detail` | `order_details` | Item pesanan + `desain_file_path` | ✔ `deleted_at` |
| `product` | `products` | Katalog produk | ✔ `deleted_at` |
| `Ukuran_Produk` | `product_sizes` | Ukuran & harga tambahan per produk | — |
| `kategory_product` | `product_categories` | Kategori produk | — |
| `status_product` | `product_statuses` | Tersedia / Habis | — |
| `status_pengerjaan` | `job_statuses` | 1 Sedang Berlangsung, 2 Dibatalkan, 3 Selesai | — |
| `status_payment` | `payment_statuses` | Status pembayaran (1..6) | — |
| `payments` | `payments` | Transaksi Midtrans | — (bukti transaksi) |
| `Layanan` | `services` | Layanan cetak (konten marketing) | — |

## 2. Relasi Antar Tabel

```
Roles 1---* User 1---* Alamat
                |         |
                |         |
                |         *
                +---* Pesanan (orders) *---1 status_pengerjaan (job_statuses)
                        |            |
                        |            *---1 status_payment (payments)
                        |
                        *---* Pesanan_Detail (order_details)
                                  |        |
                                  |        *---* product ---1 kategory_product
                                  |                 |
                                  |                 *---1 status_product
                                  |                 |
                                  |                 *---* Ukuran_Produk
                                  *---* Ukuran_Produk (ukuran terpilih)

Pesanan_Detail.idProduct -> product.id          (RESTRICT, histori tetap valid)
Pesanan_Detail.idPesanan -> Pesanan.id          (RESTRICT)
payments.idPesanan       -> Pesanan.id          (CASCADE)
```

Karena `Pesanan_Detail` memakai `RESTRICT` ke `product`, product **tidak boleh
dihapus permanen** selama masih dipakai pesanan — itulah alasan soft delete
dipakai di tabel `product`.

## 3. Kolom Audit

Semua tabel punya kolom audit berikut (nama kolom `User` dan `Pesanan` memakai
huruf besar di awal karena mengikuti struktur database yang sudah ada —
MySQL tidak membedakan besar/kecil huruf untuk nama kolom):

| Kolom | Tipe | Fungsi |
|---|---|---|
| `created_at` / `Created_At` | `TIMESTAMP NULL DEFAULT current_timestamp()` | Waktu baris dibuat |
| `updated_at` / `Updated_At` | `TIMESTAMP NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()` | Waktu baris terakhir diubah (otomatis) |
| `deleted_at` / `Deleted_At` | `TIMESTAMP NULL DEFAULT NULL` | Waktu baris di-soft delete; `NULL` = aktif |

Kolom `updated_at` **tidak pernah** diset manual oleh kode: nilainya diurus
MySQL melalui `ON UPDATE current_timestamp()`. Jadi setiap perubahan status
pesanan (`PATCH /api/v1/pesanan/{id}/status`) otomatis memperbarui
`Pesanan.Updated_At`, dan nilainya dikirim ke API sebagai `updatedAt`.

## 4. Soft Delete

### Aturan dasar

```sql
deleted_at IS NULL      -- baris aktif, ini yang selalu dibaca API
deleted_at IS NOT NULL  -- baris dianggap terhapus, tetap ada di database
```

Tidak ada query API yang menghapus baris permanen. `DELETE` pada endpoint
berikut hanya mengisi kolom `deleted_at`:

| Endpoint | Tabel | Kolom |
|---|---|---|
| `DELETE /api/v1/products/{id}` | `product` | `deleted_at` |
| `DELETE /api/v1/alamat/{id}` | `Alamat` | `deleted_at` |
| `DELETE /api/v1/pesanan/{id}` | `Pesanan` | `Deleted_At` |

`PUT /api/v1/pesanan/{id}` juga tidak menghapus `Pesanan_Detail` lama: baris
lama ditandai `deleted_at`, lalu detail baru di-insert sebagai baris aktif.

### Kenapa soft delete

1. **Histori transaksi tetap utuh.** Pesanan lama masih bisa diaudit walau
   product atau alamatnya sudah dihapus pelanggan/admin.
2. **FK `RESTRICT` tidak pernah gagal.** `Pesanan_Detail.idProduct` dan
   `idPesanan` memakai `RESTRICT`; soft delete tidak menyentuh FK tersebut.
3. **Laporan keuangan tidak berubah.** `total_harga` dan `harga_satuan` tetap
   bisa direkap karena barisnya tidak hilang.
4. **Bisa dipulihkan.** Cukup `UPDATE ... SET deleted_at = NULL` untuk
   mengembalikan data.

### Konsekuensi di kode

Semua service yang membaca tabel ber-soft-delete sudah menambahkan filter:

| File | Perubahan |
|---|---|
| `Services/ProductServices.cs` | `GetAllProductsAsync` & `UpdateProductAsync` memfilter `deleted_at IS NULL`; `DeleteProductAsync` jadi `UPDATE product SET deleted_at = NOW()` |
| `Services/AlamatServices.cs` | Semua SELECT memfilter `a.deleted_at IS NULL`; `DeleteAlamatAsync` jadi soft delete |
| `Services/PesananServices.cs` | Semua SELECT pesanan memfilter `ps.deleted_at IS NULL`; detail memfilter `d.deleted_at IS NULL`; validasi product/alamat/ukuran ikut memfilter; `DeletePesananAsync` & penggantian detail jadi soft delete |
| `Services/PaymentServices.cs` | Pembuatan/pembatalan pembayaran hanya untuk pesanan aktif |

Catatan: baris yang sudah di-soft delete dianggap "tidak ada" oleh API.
`GET /api/v1/pesanan/{id}` untuk pesanan yang sudah dihapus mengembalikan
`404`, dan `PATCH .../status` juga `404`.

## 5. DDL

DDL lengkap untuk database baru ada di [`Schema/setup.sql`](../Schema/setup.sql)
(urutan pembuatan tabel mengikuti dependency FK — jalankan dari atas ke bawah):

```bash
mysql -u <user> -p -e "CREATE DATABASE katalog_percetakan CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"
mysql -u <user> -p katalog_percetakan < Schema/setup.sql
```

Untuk database yang **sudah berjalan**, jangan jalankan `setup.sql` dari awal;
pakai migrasi yang idempotent (aman dijalankan berulang):

```bash
mysql -u <user> -p katalog_percetakan < Schema/migration_soft_delete_audit.sql
```

Migrasi tersebut menambahkan kolom audit & soft delete, index pendukung,
backfill `deleted_at = NULL`, dan seed status `Selesai` — semuanya dicek lewat
`information_schema` sehingga tidak error bila sudah ada.

### Contoh DDL inti (kutipan dari `Schema/setup.sql`)

```sql
-- product (products): soft delete + audit
CREATE TABLE IF NOT EXISTS product (
    id INT AUTO_INCREMENT,
    idKategoriProduct INT NOT NULL,
    idStatusProduct INT NOT NULL,
    nama VARCHAR(150) NOT NULL,
    deskripsi TEXT NULL,
    imagePath VARCHAR(255) NULL,
    harga DECIMAL(15,2) NOT NULL,
    background_color VARCHAR(25) NULL,
    diskon DECIMAL(15,2) NULL,
    created_at TIMESTAMP NULL DEFAULT current_timestamp(),
    updated_at TIMESTAMP NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
    deleted_at TIMESTAMP NULL DEFAULT NULL,
    PRIMARY KEY (id),
    KEY fk_product_kategory (idKategoriProduct),
    KEY fk_status_product (idStatusProduct),
    KEY idx_product_deleted (deleted_at),
    CONSTRAINT fk_product_kategory
        FOREIGN KEY (idKategoriProduct) REFERENCES kategory_product(id)
        ON UPDATE CASCADE,
    CONSTRAINT fk_status_product
        FOREIGN KEY (idStatusProduct) REFERENCES status_product(id)
        ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Pesanan (orders): audit + soft delete + index untuk daftar admin
CREATE TABLE IF NOT EXISTS Pesanan (
    id INT AUTO_INCREMENT,
    idUser INT NOT NULL,
    idAlamat INT NOT NULL,
    idStatusPengerjaan INT NOT NULL,
    total_harga DECIMAL(15,2) NOT NULL,
    Created_At TIMESTAMP NULL DEFAULT current_timestamp(),
    Updated_At TIMESTAMP NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
    Deleted_At TIMESTAMP NULL DEFAULT NULL,
    PRIMARY KEY (id),
    KEY fk_pesanan_user (idUser),
    KEY fk_pesanan_alamat (idAlamat),
    KEY fk_pesanan_status (idStatusPengerjaan),
    KEY idx_pesanan_aktif (idUser, Deleted_At),
    KEY idx_pesanan_status (idStatusPengerjaan, Deleted_At),
    CONSTRAINT fk_pesanan_user FOREIGN KEY (idUser) REFERENCES User(Id),
    CONSTRAINT fk_pesanan_alamat FOREIGN KEY (idAlamat) REFERENCES Alamat(id),
    CONSTRAINT fk_pesanan_status
        FOREIGN KEY (idStatusPengerjaan) REFERENCES status_pengerjaan(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Pesanan_Detail (order_details): menyimpan file desain + soft delete
CREATE TABLE IF NOT EXISTS Pesanan_Detail (
    id INT AUTO_INCREMENT,
    idPesanan INT NOT NULL,
    idProduct INT NOT NULL,
    idUkuranProduk INT NULL,
    ukuran_custom VARCHAR(100) NULL,
    qty INT NOT NULL DEFAULT 1,
    harga_satuan DECIMAL(15,2) NOT NULL,
    notes TEXT NULL,
    desain_file_path VARCHAR(255) NULL,
    desain_text TEXT NULL,
    created_at TIMESTAMP NULL DEFAULT current_timestamp(),
    updated_at TIMESTAMP NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
    deleted_at TIMESTAMP NULL DEFAULT NULL,
    PRIMARY KEY (id),
    KEY fk_detail_pesanan (idPesanan),
    KEY fk_detail_product (idProduct),
    KEY fk_detail_ukuran (idUkuranProduk),
    KEY idx_detail_aktif (idPesanan, deleted_at),
    CONSTRAINT fk_detail_pesanan FOREIGN KEY (idPesanan) REFERENCES Pesanan(id),
    CONSTRAINT fk_detail_product FOREIGN KEY (idProduct) REFERENCES product(id),
    CONSTRAINT fk_detail_ukuran FOREIGN KEY (idUkuranProduk) REFERENCES Ukuran_Produk(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

### Index penting

| Index | Dipakai untuk |
|---|---|
| `idx_pesanan_aktif (idUser, Deleted_At)` | Daftar pesanan pelanggan (`GET /api/v1/pesanan`) |
| `idx_pesanan_status (idStatusPengerjaan, Deleted_At)` | Filter status di layar admin |
| `idx_detail_aktif (idPesanan, deleted_at)` | Pengambilan `details[]` per pesanan |
| `idx_alamat_aktif (idUser, deleted_at)` | Daftar alamat aktif per pelanggan |
| `idx_product_deleted (deleted_at)` | Katalog product aktif |

## 6. Contoh Payload API

### 6.1. Ambil daftar pesanan (layar admin)

Hanya pesanan aktif (`Deleted_At IS NULL`) yang dikembalikan, urut `id`
terbaru di depan, dan setiap `details[]` sudah membawa `desainFilePath`.

**Request**

```http
GET /api/v1/pesanan/all HTTP/1.1
Authorization: Bearer <token-admin-atau-petugas>
```

```js
const res = await fetch(`${API}/api/v1/pesanan/all`, {
  headers: { Authorization: `Bearer ${token}` },
});
const pesanan = await res.json(); // PesananResponse[]
```

**Response 200**

```json
[
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
        "idUkuranProduk": 3,
        "namaUkuran": "A3",
        "ukuranCustom": null,
        "qty": 1,
        "hargaSatuan": 150000.00,
        "notes": "Cetak warna",
        "desainFilePath": "/images/desain/0f1c9a5e-2b4c-4d5e-9a7b-3c1d2e4f5a6b.png",
        "desainText": "Selamat Ulang Tahun"
      }
    ]
  },
  {
    "id": 11,
    "idUser": 7,
    "namaUser": "Habib Herdiansyah",
    "idAlamat": 5,
    "alamat": "Koto Tuo, Payakumbuh, West Sumatra, 26218, Indonesia",
    "idStatusPengerjaan": 1,
    "statusPengerjaan": "Sedang Berlangsung",
    "totalHarga": 50000.00,
    "paymentStatus": "unpaid",
    "createdAt": "2026-09-16T14:02:11+07:00",
    "updatedAt": "2026-09-16T14:02:11+07:00",
    "details": [
      {
        "id": 29,
        "idPesanan": 11,
        "idProduct": 2,
        "namaProduct": "Spanduk",
        "idUkuranProduk": null,
        "namaUkuran": null,
        "ukuranCustom": "2 x 1 m",
        "qty": 2,
        "hargaSatuan": 25000.00,
        "notes": null,
        "desainFilePath": null,
        "desainText": "Diskon 20%"
      }
    ]
  }
]
```

Aturan `details[].desainFilePath`:

| Kondisi | Nilai di response |
|---|---|
| `desain_file_path` NULL / kosong | `null` |
| File ada di `wwwroot` | path media, mis. `/images/desain/<uuid>.png` |
| Path tersimpan tetapi file sudah tidak ada | `null` (tidak ada link rusak) |
| URL absolut (`http`/`https`, mis. CDN) | dikembalikan apa adanya |

File desain dilayani `app.UseStaticFiles()`, jadi frontend memakainya seperti
`<img src={`${API}${detail.desainFilePath}`} />`.

**Error** — `401` (tanpa token), `403` (role `Pelanggan`).

### 6.2. Ubah status pengerjaan pesanan (khusus Admin)

Endpoint ini yang menulis `Pesanan.idStatusPengerjaan` secara manual (bukan
hanya otomatis dari pembayaran). Bisa memakai **id** atau **nama** status.

**Request — pakai nama status**

```http
PATCH /api/v1/pesanan/12/status HTTP/1.1
Authorization: Bearer <token-admin>
Content-Type: application/json

{ "statusPengerjaan": "Selesai" }
```

**Request — pakai id status**

```http
PATCH /api/v1/pesanan/12/status HTTP/1.1
Authorization: Bearer <token-admin>
Content-Type: application/json

{ "idStatusPengerjaan": 3 }
```

```js
await fetch(`${API}/api/v1/pesanan/${id}/status`, {
  method: "PATCH",
  headers: {
    "Content-Type": "application/json",
    Authorization: `Bearer ${token}`,
  },
  body: JSON.stringify({ statusPengerjaan: "Selesai" }),
});
```

**Response 200** — `PesananResponse` terbaru (termasuk `details[]` &
`desainFilePath`), siap dipakai untuk me-refresh baris tabel tanpa request
tambahan:

```json
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
      "idUkuranProduk": 3,
      "namaUkuran": "A3",
      "ukuranCustom": null,
      "qty": 1,
      "hargaSatuan": 150000.00,
      "notes": "Cetak warna",
      "desainFilePath": "/images/desain/0f1c9a5e-2b4c-4d5e-9a7b-3c1d2e4f5a6b.png",
      "desainText": "Selamat Ulang Tahun"
    }
  ]
}
```

**Error**

```json
// 400 - body kosong / kedua field kosong
{ "message": "IdStatusPengerjaan atau StatusPengerjaan wajib diisi" }

// 400 - nama status tidak ada di tabel status_pengerjaan
{ "message": "Status pengerjaan 'Nganggur' tidak ditemukan" }

// 400 - id status tidak ada
{ "message": "Status pengerjaan 99 tidak ditemukan" }

// 404 - pesanan tidak ada / sudah di-soft delete
{ "message": "Pesanan tidak ditemukan" }
```

`401` bila token tidak ada, dan `403` bila token bukan role `Admin`.

Status yang tersedia diambil dari `GET /api/v1/status-pengerjaan`
(seed: `1 Sedang Berlangsung`, `2 Dibatalkan`, `3 Selesai`).

## 7. Catatan Implementasi

| Topik | Lokasi kode |
|---|---|
| Filter soft delete + resolusi `desainFilePath` | `Services/PesananServices.cs` (`ResolveDesainFilePath`, `ApplyDesainFilePath`) |
| Soft delete product | `Services/ProductServices.cs` (`DeleteProductAsync`) |
| Soft delete alamat | `Services/AlamatServices.cs` (`DeleteAlamatAsync`) |
| Soft delete pesanan | `Services/PesananServices.cs` (`DeletePesananAsync`) |
| Ubah status pengerjaan + otorisasi Admin | `Controller/PesananController.cs` (`PATCH /{id}/status`, `Policies.Admin`) + `Services/PesananServices.cs` (`UpdateStatusPengerjaanAsync`) |
| Model request/response | `Models/Pesanan.cs` (`PesananStatusRequest`, `PesananStatusResult`) |
| Upload file desain | `Controller/PesananController.cs` (`SaveDesainFilesAsync`, `NormalizeDesainPath`) |

Hal yang perlu diperhatikan:

- **Migrasi wajib sebelum deploy.** Kode terbaru membaca `deleted_at` dan
  `Updated_At`; jalankan `Schema/migration_soft_delete_audit.sql` lebih dulu.
- **Status `Selesai` harus ada** di tabel `status_pengerjaan`. Migrasi sudah
  menambahkan seed `(3, 'Selesai')`.
- **`items[N].desainFilePath` disaring server.** Hanya nilai yang diawali
  `/images/desain/` yang diterima; selain itu (termasuk `../`) diabaikan
  sehingga tidak bisa dipakai untuk menulis path sembarang ke database.
- **Pesanan yang sudah punya pembayaran** tetap tidak bisa diubah/dihapus
  lewat `PUT`/`DELETE` (aturan lama), terlepas dari soft delete.
- **Product ter-soft delete tidak bisa dipakai pesanan baru** — `ProductExistsAsync`
  dan perhitungan harga ikut memfilter `deleted_at IS NULL`.
