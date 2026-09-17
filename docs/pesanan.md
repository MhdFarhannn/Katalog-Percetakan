# Pesanan

Base path: `/api/v1/pesanan` — semua endpoint **butuh Bearer token**.
`idUser` diambil dari JWT, bukan dari body.

## Ringkasan Endpoint

| Method | Endpoint | Akses |
|---|---|---|
| POST | `/api/v1/pesanan` | Pelanggan (pemilik) |
| GET | `/api/v1/pesanan` | Pesanan milik sendiri |
| GET | `/api/v1/pesanan/all` | `Admin` / `Petugas` |
| GET | `/api/v1/pesanan/{id}` | Pemilik, atau `Admin`/`Petugas` |
| PUT | `/api/v1/pesanan/{id}` | Pemilik, hanya bila **belum ada pembayaran** |
| DELETE | `/api/v1/pesanan/{id}` | Pemilik, hanya bila **belum ada pembayaran** |

## Model

### `PesananRequest`

```json
{
  "idAlamat": 5,
  "items": [
    {
      "idProduct": 5,
      "idUkuranProduk": null,
      "ukuranCustom": "A3",
      "qty": 1,
      "notes": "Cetak warna",
      "desainFilePath": null,
      "desainText": "Selamat Ulang Tahun"
    }
  ]
}
```

| Field | Tipe | Wajib | Keterangan |
|---|---|---|---|
| `idAlamat` | int | Ya | Harus milik user yang login |
| `items` | array | Ya | Minimal 1 item |
| `items[].idProduct` | int | Ya | Product harus ada |
| `items[].idUkuranProduk` | int? | Tidak | Harus milik product terkait |
| `items[].ukuranCustom` | string? | Tidak | Ukuran bebas |
| `items[].qty` | int | Ya | Minimal 1 |
| `items[].notes` | string? | Tidak | Catatan item |
| `items[].desainFilePath` | string? | Tidak | Path file desain |
| `items[].desainText` | string? | Tidak | Teks desain |

> `hargaSatuan` dan `totalHarga` **dihitung server** dari `product.harga`
> (+ `Ukuran_Produk.harga_tambahan`). Frontend **tidak** mengirim harga.

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

**Response 200** — `PesananResponse`.

**Error `400`**

```json
{ "message": "IdAlamat wajib diisi" }
{ "message": "Alamat tidak ditemukan" }
{ "message": "Pesanan minimal memiliki 1 item" }
{ "message": "IdProduct wajib diisi" }
{ "message": "Qty minimal 1" }
{ "message": "Produk 999 tidak ditemukan" }
{ "message": "Ukuran tidak sesuai dengan produk 5" }
```

```js
const res = await fetch(`${API}/api/v1/pesanan`, {
  method: "POST",
  headers: {
    "Content-Type": "application/json",
    Authorization: `Bearer ${token}`,
  },
  body: JSON.stringify({
    idAlamat: 5,
    items: [{ idProduct: 5, qty: 2, notes: "Cetak warna" }],
  }),
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

Body sama seperti POST. Mengganti seluruh `items` (detail lama dihapus).

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
