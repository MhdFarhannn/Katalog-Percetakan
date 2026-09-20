# Pesanan

Base path: `/api/v1/pesanan` — semua endpoint **butuh Bearer token**.
`idUser` diambil dari JWT, bukan dari body.

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
| GET | `/api/v1/pesanan/all` | `Admin` / `Petugas` | — |
| GET | `/api/v1/pesanan/{id}` | Pemilik, atau `Admin`/`Petugas` | — |
| PUT | `/api/v1/pesanan/{id}` | Pemilik, hanya bila **belum ada pembayaran** | `multipart/form-data` |
| DELETE | `/api/v1/pesanan/{id}` | Pemilik, hanya bila **belum ada pembayaran** | — |

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
pembayaran (`pending`, `paid`, dst — lihat [README](README.md#status-pembayaran)).

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

## GET /api/v1/pesanan/{id}

**Response 200** — `PesananResponse` (termasuk `details`).
**Error `404`** — `{ "message": "Pesanan tidak ditemukan" }`.

## PUT /api/v1/pesanan/{id}

Field form **sama seperti POST** (multipart/form-data, bukan JSON).
Mengganti seluruh `items` (detail lama dihapus).

**Response 200** — `PesananResponse` terbaru.

**Error `400`**

```json
{ "message": "Pesanan tidak ditemukan atau sudah memiliki pembayaran" }
```

## DELETE /api/v1/pesanan/{id}

**Response 200**

```json
{ "message": "Pesanan berhasil dihapus" }
```

**Error `400`** — `{ "message": "Pesanan tidak ditemukan atau sudah memiliki pembayaran" }`.

> Setelah pesanan dibuat, lanjutkan ke [payment.md](payment.md) untuk
> membuat transaksi Midtrans.
>
> File desain yang diupload via `items[N].desain` tersimpan di server dan
> path-nya muncul di response `details[].desainFilePath`
> (mis. `/images/desain/<uuid>.png`).
