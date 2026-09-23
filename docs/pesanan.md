# Pesanan

Base path: `/api/v1/pesanan` — semua endpoint **butuh Bearer token**.
`idUser` diambil dari JWT, bukan dari body.

> Endpoint mana yang dipakai frontend admin dan mana yang dipakai pelanggan:
> lihat [admin.md](admin.md) dan [pelanggan.md](pelanggan.md).

> **Aturan untuk AI Agent:** endpoint **POST** dan **PUT** pesanan **wajib**
> memakai **`Content-Type: multipart/form-data`**. **JANGAN PERNAH** mengirim
> body JSON dan **JANGAN** memakai `Content-Type: application/json` untuk
> endpoint ini. Kirim setiap parameter sebagai form field terpisah dengan nama
> persis seperti di bawah. File desain dikirim sebagai **multipart file field**
> (bukan Base64, bukan JSON). Boundary **tidak** ditulis manual — biarkan
> HTTP client/library yang membuatnya otomatis. Pastikan semua field wajib
> tersedia sebelum mengirim request; jika ada yang kurang, tanyakan dulu
> kepada user, jangan kirim request.

## Ringkasan Endpoint

| Method | Endpoint | Akses | Content-Type |
|---|---|---|---|
| POST | `/api/v1/pesanan` | Pelanggan (pemilik) | `multipart/form-data` |
| GET | `/api/v1/pesanan` | Pesanan milik sendiri | — |
| GET | `/api/v1/pesanan/history` | Semua role — filter tanggal/status/paginasi (lihat [laporan.md](laporan.md)) | — |
| GET | `/api/v1/pesanan/all` | `Admin` / `Petugas` | — |
| GET | `/api/v1/pesanan/{id}` | Pemilik, atau `Admin`/`Petugas` | — |
| PUT | `/api/v1/pesanan/{id}` | Pemilik, hanya bila **belum ada pembayaran** | `multipart/form-data` |
| PUT | `/api/v1/pesanan/{id}/status` | `Admin` (ubah status pengerjaan) | `application/json` |
| DELETE | `/api/v1/pesanan/{id}` | Pemilik, hanya bila **belum ada pembayaran** | — |

> Pembayaran yang dibatalkan **tetap tercatat** di tabel `payments`, sehingga
> pesanan yang sudah pernah dibuatkan pembayaran tidak bisa lagi diubah (`PUT`)
> atau dihapus (`DELETE`).
>
> `DELETE /api/v1/pesanan/{id}` adalah **soft delete**: baris `Pesanan` dan
> `Pesanan_Detail` tidak dihapus permanen, hanya diisi `Deleted_At` /
> `deleted_at`. Setelah di-soft delete, pesanan tidak muncul lagi di endpoint
> GET mana pun dan `GET /api/v1/pesanan/{id}` mengembalikan `404`.

## Model

### `PesananRequest`

Request POST/PUT dikirim sebagai **`multipart/form-data`** (bukan JSON).
Setiap parameter adalah form field terpisah dengan nama persis seperti di
bawah — termasuk field di dalam `items[N]` (index mulai dari `0`).

```http
POST /api/v1/pesanan HTTP/1.1
Authorization: Bearer <token>
Content-Type: multipart/form-data; boundary=---dibuat-otomatis-oleh-client

---dibuat-otomatis-oleh-client
Content-Disposition: form-data; name="idAlamat"

5
---dibuat-otomatis-oleh-client
Content-Disposition: form-data; name="items[0].idProduct"

5
---dibuat-otomatis-oleh-client
Content-Disposition: form-data; name="items[0].idUkuranProduk"


---dibuat-otomatis-oleh-client
Content-Disposition: form-data; name="items[0].ukuranCustom"

A3
---dibuat-otomatis-oleh-client
Content-Disposition: form-data; name="items[0].qty"

1
---dibuat-otomatis-oleh-client
Content-Disposition: form-data; name="items[0].notes"

Cetak warna
---dibuat-otomatis-oleh-client
Content-Disposition: form-data; name="items[0].desain"; filename="desain.png"
Content-Type: image/png

<binary file>
---dibuat-otomatis-oleh-client
Content-Disposition: form-data; name="items[0].desainText"

Selamat Ulang Tahun
---dibuat-otomatis-oleh-client--
```

| Form field | Tipe | Wajib | Keterangan |
|---|---|---|---|
| `idAlamat` | int | Ya | Harus milik user yang login |
| `items[0].idProduct` | int | Ya | Product harus ada. Minimal 1 item (`items[0]`) |
| `items[N].idProduct` | int | Ya | Item tambahan, index berurutan mulai `1` |
| `items[N].idUkuranProduk` | int? | Tidak | Harus milik product terkait. Kosongkan bila tidak dipakai |
| `items[N].ukuranCustom` | string? | Tidak | Ukuran bebas |
| `items[N].qty` | int | Ya | Minimal 1 |
| `items[N].notes` | string? | Tidak | Catatan item |
| `items[N].desain` | file | Tidak | **File desain** dikirim sebagai multipart file field (bukan Base64, bukan JSON) |
| `items[N].desainText` | string? | Tidak | Teks desain |

> **Jangan kirim field `items[N].desainFilePath`.** Path file desain
> (`desainFilePath`) **dibuat server** dari file multipart yang diupload
> (`items[N].desain`) dan hanya muncul di response.
>
> `hargaSatuan` dan `totalHarga` juga **dihitung server** dari
> `product.harga` (+ `Ukuran_Produk.harga_tambahan`). Frontend **tidak**
> mengirim harga.

### `PesananResponse`

```json
{
  "id": 4,
  "idUser": 2,
  "namaUser": "Habib Herdiansyah",
  "idAlamat": 5,
  "alamat": "Koto Tuo, Payakumbuh, West Sumatra, 26218, Indonesia",
  "idStatusPengerjaan": 1,
  "statusPengerjaan": "Sedang Berlangsung",
  "totalHarga": 100000.00,
  "paymentStatus": "unpaid",
  "createdAt": "2026-09-17T08:18:46+07:00",
  "updatedAt": "2026-09-17T08:18:46+07:00",
  "details": [
    {
      "id": 6,
      "idPesanan": 4,
      "idProduct": 5,
      "namaProduct": "Bodi Bakar",
      "idUkuranProduk": null,
      "namaUkuran": null,
      "ukuranCustom": null,
      "qty": 1,
      "hargaSatuan": 100000.00,
      "notes": "Cetak warna",
      "desainFilePath": null,
      "desainText": "Selamat Ulang Tahun"
    }
  ]
}
```

`paymentStatus` bisa `unpaid` (belum ada pembayaran) atau salah satu status
pembayaran (`pending`, `paid`, `cancelled`, `expired`, dst — lihat
[README](README.md#status-pembayaran)).

Nilai tersebut diambil dari `payments.idStatusPayment` pembayaran terakhir
pesanan (JOIN ke tabel master `status_payment`), lalu kodenya dipetakan di
C# lewat `PaymentStatusMap`. Pesanan yang belum punya baris di `payments`
tetap `unpaid`.

Ketiga endpoint pembacaan (`GET /api/v1/pesanan`, `GET /api/v1/pesanan/all`,
`GET /api/v1/pesanan/{id}`) sekaligus menyinkronkan pembayaran yang masih
`pending` ke Midtrans (`GET /v2/{order_id}/status`) sebelum response dikirim.
Jadi status pesanan yang baru dibayar langsung berubah menjadi `paid` pada
refresh berikutnya, tanpa menunggu notifikasi webhook. Bila Midtrans tidak
dapat dihubungi, daftar pesanan tetap dikembalikan dengan status yang tersimpan
di database.

`statusPengerjaan` bernilai `Sedang Berlangsung`, atau `Dibatalkan` bila
pembayaran pesanan dibatalkan oleh pelanggan atau kedaluwarsa (lihat
[payment.md](payment.md#post-apiv1paymentidpesanancancel)).

---

## POST /api/v1/pesanan

**Content-Type: `multipart/form-data`** — JANGAN kirim JSON.

Kirim setiap parameter sebagai form field terpisah:

```
Content-Type: multipart/form-data

idAlamat: 5
items[0].idProduct: 5
items[0].qty: 2
items[0].notes: Cetak warna
items[0].desain: (file, opsional)
```

**Response 200** — `PesananResponse`.

**Error `400`**

```json
{ "message": "Content-Type harus multipart/form-data" }
{ "message": "IdAlamat wajib diisi" }
{ "message": "Alamat tidak ditemukan" }
{ "message": "Pesanan minimal memiliki 1 item" }
{ "message": "IdProduct wajib diisi" }
{ "message": "Qty minimal 1" }
{ "message": "Produk 999 tidak ditemukan" }
{ "message": "Ukuran tidak sesuai dengan produk 5" }
```

```js
// Gunakan FormData agar browser/library membuat boundary multipart
// OTOMATIS. JANGAN set header Content-Type secara manual.
const form = new FormData();
form.append("idAlamat", "5");
form.append("items[0].idProduct", "5");
form.append("items[0].qty", "2");
form.append("items[0].notes", "Cetak warna");
// form.append("items[0].desain", fileInput.files[0]); // file desain (opsional)
form.append("items[0].desainText", "Selamat Ulang Tahun");

const res = await fetch(`${API}/api/v1/pesanan`, {
  method: "POST",
  headers: {
    // TANPA "Content-Type" — browser yang set multipart/form-data + boundary
    Authorization: `Bearer ${token}`,
  },
  body: form, // BUKAN JSON.stringify
});
const pesanan = await res.json(); // pesanan.id dipakai untuk pembayaran
```

## GET /api/v1/pesanan

**Response 200** — `PesananResponse[]` milik user login, urut `id` terbaru.

## GET /api/v1/pesanan/all

**Response 200** — seluruh pesanan (Admin/Petugas).
**Error `403`** — Pelanggan.

## GET /api/v1/pesanan/history (Order History)

Ringkasan parameter, contoh request/response, dan error ada di
[laporan.md](laporan.md#order-history). Endpoint ini mengembalikan pesanan
**aktif** (`Deleted_At IS NULL`) milik user login (pelanggan) atau seluruh
pesanan (Admin/Petugas, bisa difilter `idUser`), dengan filter:

| Query parameter | Tipe | Wajib | Keterangan |
|---|---|---|---|
| `startDate` | string `yyyy-MM-dd` | Tidak | Rentang awal (inclusive) |
| `endDate` | string `yyyy-MM-dd` | Tidak | Rentang akhir (inclusive) |
| `idStatusPengerjaan` | int | Tidak | Filter status pengerjaan |
| `idUser` | int | Tidak | Khusus `Admin`/`Petugas`; diabaikan untuk pelanggan |
| `page` | int | Tidak | Default `1` |
| `pageSize` | int | Tidak | Default `10`, maksimal `100` |

**Response 200**

```json
{
  "items": [ { "...": "PesananResponse[]" } ],
  "total": 57,
  "page": 1,
  "pageSize": 10
}
```

**Error `400`**

```json
{ "message": "Format startDate harus yyyy-MM-dd" }
{ "message": "Format endDate harus yyyy-MM-dd" }
```

## GET /api/v1/pesanan/{id}

**Response 200** — `PesananResponse` (termasuk `details`).
**Error `404`** — `{ "message": "Pesanan tidak ditemukan" }`.

## PUT /api/v1/pesanan/{id}

Field form **sama seperti POST** (multipart/form-data, bukan JSON).
Mengganti seluruh `items` (detail lama di-soft delete, lalu detail baru
di-insert sebagai baris aktif).

**Response 200** — `PesananResponse` terbaru.

**Error `400`**

```json
{ "message": "Pesanan tidak ditemukan atau sudah memiliki pembayaran" }
```

## PUT /api/v1/pesanan/{id}/status

**Akses: hanya `Admin`** (`403` untuk `Petugas`/`Pelanggan`).
**Content-Type: `application/json`** — bukan multipart.

Ubah `idStatusPengerjaan` sebuah pesanan. Detail lengkap (request, response,
error) ada di [laporan.md](laporan.md#put-apiv1pesananidstatus-admin).

**Request**

```http
PUT /api/v1/pesanan/12/status HTTP/1.1
Authorization: Bearer <token-admin>
Content-Type: application/json

{ "statusPengerjaan": "Selesai" }
```

atau pakai id:

```json
{ "idStatusPengerjaan": 3 }
```

Minimal salah satu field wajib diisi, dan status harus ada di tabel
`status_pengerjaan`.

**Response 200** — `PesananResponse` terbaru (termasuk `updatedAt`).

**Error**

```json
// 400 - body kosong / kedua field kosong
{ "message": "IdStatusPengerjaan atau StatusPengerjaan wajib diisi" }

// 400 - status tidak ada di master
{ "message": "Status pengerjaan 99 tidak ditemukan" }
{ "message": "Status pengerjaan 'Nganggur' tidak ditemukan" }

// 404 - pesanan tidak ada / sudah di-soft delete
{ "message": "Pesanan tidak ditemukan" }

// 403 - bukan role Admin
```

Status yang tersedia: `GET /api/v1/status-pengerjaan`
(seed: `1 Sedang Berlangsung`, `2 Dibatalkan`, `3 Selesai`).

## DELETE /api/v1/pesanan/{id}

**Response 200**

```json
{ "message": "Pesanan berhasil dihapus" }
```

**Error `400`** — `{ "message": "Pesanan tidak ditemukan atau sudah memiliki pembayaran" }`.

> DELETE di sini adalah **soft delete**: `Pesanan.Deleted_At` dan
> `Pesanan_Detail.deleted_at` diisi waktu hapus; baris tetap ada di database
> agar histori & laporan tetap konsisten (lihat
> [schema.md](schema.md#4-soft-delete)).

> Setelah pesanan dibuat, lanjutkan ke [payment.md](payment.md) untuk
> membuat transaksi Midtrans.
>
> File desain yang diupload via `items[N].desain` tersimpan di server dan
> path-nya muncul di response `details[].desainFilePath`
> (mis. `/images/desain/<uuid>.png`).
