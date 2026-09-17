# Master Data

Berisi endpoint kategori product, status product, status pengerjaan, product,
dan layanan.

> **Catatan otorisasi (sesuai kode saat ini):**
> - `kategory-product`, `status-product`, `status-pengerjaan`: **tanpa auth**.
> - `products`: `GET` publik; `POST` butuh role `Admin`; `PUT`/`DELETE` tanpa auth.
> - `layanan`: butuh auth (`Admin`/`Petugas`/`Pelanggan`).

---

# Kategori Product

Base path: `/api/v1/kategory-product`

| Method | Endpoint | Body |
|---|---|---|
| GET | `/api/v1/kategory-product` | — |
| POST | `/api/v1/kategory-product` | `{ "nama": "Percetakan" }` |
| PATCH | `/api/v1/kategory-product/{id}` | `{ "nama": "Percetakan Baru" }` |
| DELETE | `/api/v1/kategory-product/{id}` | — |

**GET Response 200**

```json
[
  { "id": 2, "nama": "Percetakan" },
  { "id": 3, "nama": "Packaging" }
]
```

**POST/PATCH/DELETE Response 200** — body kosong.

---

# Status Product

Base path: `/api/v1/status-product`

| Method | Endpoint | Body |
|---|---|---|
| GET | `/api/v1/status-product` | — |
| POST | `/api/v1/status-product` | `{ "nama": "Tersedia" }` |
| PATCH | `/api/v1/status-product/{id}` | `{ "nama": "Habis" }` |
| DELETE | `/api/v1/status-product/{id}` | — |

**GET Response 200**

```json
[
  { "id": 2, "nama": "Tersedia" },
  { "id": 3, "nama": "Habis" }
]
```

---

# Status Pengerjaan

Base path: `/api/v1/status-pengerjaan`

| Method | Endpoint | Body |
|---|---|---|
| GET | `/api/v1/status-pengerjaan` | — |
| POST | `/api/v1/status-pengerjaan` | `{ "nama": "Sedang Berlangsung" }` |
| PATCH | `/api/v1/status-pengerjaan/{id}` | `{ "nama": "..." }` |
| DELETE | `/api/v1/status-pengerjaan/{id}` | — |

**GET Response 200**

```json
[
  { "id": 1, "nama": "Sedang Berlangsung" }
]
```

---

# Product

Base path: `/api/v1/products`

| Method | Endpoint | Auth | Content-Type |
|---|---|---|---|
| GET | `/api/v1/products` | Tidak | — |
| POST | `/api/v1/products` | `Admin` | `multipart/form-data` |
| PUT | `/api/v1/products/{id}` | Tidak | `application/json` |
| DELETE | `/api/v1/products/{id}` | Tidak | — |

## GET /api/v1/products

**Response 200**

```json
[
  {
    "id": 5,
    "idKategoriProduct": 2,
    "idStatusProduct": 2,
    "nama": "Bodi Bakar",
    "deskripsi": "Cetak bodi motor",
    "imagePath": "/images/abc.png",
    "harga": 100000.00,
    "diskon": 0,
    "backgroundColor": null,
    "kategoryProduct": { "id": 2, "nama": "Percetakan" },
    "statusProduct": { "id": 2, "nama": "Tersedia" }
  }
]
```

## POST /api/v1/products

**Form fields (multipart/form-data)**

| Field | Tipe | Wajib |
|---|---|---|
| `IdKategoriProduct` | int | Ya |
| `IdStatusProduct` | int | Ya |
| `Nama` | string | Ya |
| `Deskripsi` | string | Tidak |
| `Harga` | decimal | Ya |
| `BackgroundColor` | string | Tidak |
| `Image` | file | Tidak |

**Response 200** — mengembalikan payload product yang dikirim.
Catatan: `id` **belum terisi** pada response (server belum mengembalikan id).

```js
const form = new FormData();
form.append("IdKategoriProduct", 2);
form.append("IdStatusProduct", 2);
form.append("Nama", "Bodi Bakar");
form.append("Deskripsi", "Cetak bodi motor");
form.append("Harga", 100000);
form.append("Image", fileInput.files[0]);

await fetch(`${API}/api/v1/products`, {
  method: "POST",
  headers: { Authorization: `Bearer ${token}` }, // jangan set Content-Type manual
  body: form,
});
```

## PUT /api/v1/products/{id}

**Request (JSON)**

```json
{
  "idKategoriProduct": 2,
  "idStatusProduct": 2,
  "nama": "Bodi Bakar",
  "deskripsi": "Cetak bodi motor",
  "imagePath": "/images/abc.png",
  "harga": 120000,
  "backgroundColor": null
}
```

**Response 200** — body kosong. **404** bila product tidak ditemukan.

## DELETE /api/v1/products/{id}

**Response 200** — body kosong. **404** bila tidak ditemukan.

---

# Layanan

Base path: `/api/v1/layanan` — **butuh Bearer token**.

| Method | Endpoint | Content-Type |
|---|---|---|
| GET | `/api/v1/layanan` | — |
| GET | `/api/v1/layanan/{id}` | — |
| POST | `/api/v1/layanan` | `multipart/form-data` |
| PATCH | `/api/v1/layanan/{id}` | `multipart/form-data` |
| DELETE | `/api/v1/layanan/{id}` | — |

**Model**

```json
{
  "id": 1,
  "nama": "Cetak Spanduk",
  "deskripsi": "Spanduk berbagai ukuran",
  "imagePath": "/images/layanan/xxx.jpg",
  "backgroundColor": "#ffffff"
}
```

**Form fields POST/PATCH:** `Nama`, `Deskripsi`, `BackgroundColor`, `Image` (file).

**Error:** `400` bila `Nama`/`Deskripsi` kosong, `404` bila data tidak ditemukan.

```js
const form = new FormData();
form.append("Nama", "Cetak Spanduk");
form.append("Deskripsi", "Spanduk berbagai ukuran");
form.append("BackgroundColor", "#ffffff");
form.append("Image", fileInput.files[0]);

await fetch(`${API}/api/v1/layanan`, {
  method: "POST",
  headers: { Authorization: `Bearer ${token}` },
  body: form,
});
```
