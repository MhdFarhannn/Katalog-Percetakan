# Alamat

Base path: `/api/v1/alamat` — semua endpoint **butuh Bearer token**
(`Admin`/`Petugas`/`Pelanggan`).

`idUser` **tidak** dikirim dari body; server mengambilnya dari JWT.

## Ringkasan Endpoint

| Method | Endpoint | Keterangan |
|---|---|---|
| GET | `/api/v1/alamat` | Alamat milik user yang login |
| GET | `/api/v1/alamat/all` | Semua alamat (Admin/Petugas) |
| GET | `/api/v1/alamat/user/{idUser}` | Alamat milik user tertentu |
| POST | `/api/v1/alamat` | Tambah alamat (milik user login) |
| PATCH | `/api/v1/alamat/{id}` | Ubah alamat milik sendiri |
| DELETE | `/api/v1/alamat/{id}` | Hapus alamat milik sendiri |

## Model Response — `AlamatResponse`

```json
{
  "id": 5,
  "idUser": 2,
  "namaUser": "Habib Herdiansyah",
  "noTelepon": "081234567890",
  "content": "Koto Tuo, Payakumbuh, West Sumatra, 26218, Indonesia"
}
```

## GET /api/v1/alamat

Mengembalikan `AlamatResponse[]` milik user dari token.

```js
const res = await fetch(`${API}/api/v1/alamat`, {
  headers: { Authorization: `Bearer ${token}` },
});
const alamat = await res.json();
```

## GET /api/v1/alamat/all

Mengembalikan seluruh alamat (untuk halaman admin/petugas).

## GET /api/v1/alamat/user/{idUser}

Mengembalikan alamat milik `idUser` tertentu.

## POST /api/v1/alamat

**Request**

```json
{
  "content": "Jl. Merdeka No. 10, Padang",
  "noTelepon": "081234567890"
}
```

**Response 200** — `AlamatResponse` yang baru dibuat.

**Error `400`**

```json
{ "message": "Content alamat wajib diisi" }
```

```json
{ "message": "NoTelepon wajib diisi" }
```

```js
await fetch(`${API}/api/v1/alamat`, {
  method: "POST",
  headers: {
    "Content-Type": "application/json",
    Authorization: `Bearer ${token}`,
  },
  body: JSON.stringify({ content, noTelepon }),
});
```

## PATCH /api/v1/alamat/{id}

Body sama seperti POST. User hanya bisa mengubah alamat miliknya.

**Response 200** — `AlamatResponse` terbaru.
**Error `404`** — `{ "message": "Alamat tidak ditemukan" }`.

## DELETE /api/v1/alamat/{id}

**Response 200**

```json
{ "message": "Alamat berhasil dihapus" }
```

**Error `404`** — `{ "message": "Alamat tidak ditemukan" }`.

> `idAlamat` dari endpoint ini dipakai saat membuat pesanan (`idAlamat`
> pada `POST /api/v1/pesanan`).
