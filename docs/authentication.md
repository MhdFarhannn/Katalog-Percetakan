# Authentication

Base path: `/api/v1/auth`

## Ringkasan Endpoint

| Method | Endpoint | Auth | Keterangan |
|---|---|---|---|
| POST | `/api/v1/auth/register-admin` | Tidak | Hanya sekali (dinonaktifkan setelah ada user) |
| POST | `/api/v1/auth/login` | Tidak | Login email + password |
| POST | `/api/v1/auth/google` | Tidak | Login/registrasi dengan Google |
| GET | `/api/v1/auth/me` | Bearer | Profil user yang sedang login |

---

## POST /api/v1/auth/login

Login dengan email dan password (provider `local`).

**Request**

```json
{
  "email": "habibherdiansyah08@gmail.com",
  "password": "rahasia123"
}
```

**Response 200**

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "c29tZS1yYW5kb20tc3RyaW5n...",
  "nama": "Habib Herdiansyah",
  "role": "Pelanggan"
}
```

**Error**

| Status | Kondisi |
|---|---|
| `401` | Email tidak terdaftar / password salah / user tidak aktif |
| `400` | Akun terdaftar via Google (`"Akun ini terdaftar via Google. Silakan login dengan Google."`) |
| `500` | Kesalahan server |

**Contoh**

```js
const res = await fetch(`${API}/api/v1/auth/login`, {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({ email, password }),
});
const data = await res.json();
localStorage.setItem("token", data.token);
```

---

## POST /api/v1/auth/google

Login atau registrasi otomatis via Google. Role default: `Pelanggan` (Id_Role = 3).

**Request**

```json
{
  "idToken": "GOOGLE_ID_TOKEN_DARI_FRONTEND"
}
```

**Response 200** — sama seperti login:

```json
{
  "token": "...",
  "refreshToken": "...",
  "nama": "Habib Herdiansyah",
  "role": "Pelanggan"
}
```

**Error:** `401` bila ID token Google tidak valid.

---

## POST /api/v1/auth/register-admin

Membuat user admin pertama. **Hanya bisa dipakai satu kali.**
Setelah ada user di database, endpoint mengembalikan `400` dengan body
`"API Have Been Disabled"`.

**Request**

```json
{
  "nama": "AdminCetak",
  "email": "AdminCetak@gmail.com",
  "password": "rahasia123",
  "id_Role": 1,
  "is_Active": true,
  "external_Id": ""
}
```

> Catatan: `id_Role` diabaikan oleh server; admin selalu dibuat dengan role `1`.

**Response 200** — body kosong.

---

## GET /api/v1/auth/me

Mengambil profil user dari JWT. Butuh header `Authorization`.

**Response 200**

```json
{
  "id": 2,
  "nama": "Habib Herdiansyah",
  "idRole": 3,
  "role": "Pelanggan",
  "email": "habibherdiansyah08@gmail.com",
  "authProvider": "local",
  "isActive": true,
  "noTelepon": null
}
```

**Error:** `401` bila token tidak ada / tidak valid, `404` bila user tidak ditemukan.

**Contoh**

```js
const res = await fetch(`${API}/api/v1/auth/me`, {
  headers: { Authorization: `Bearer ${localStorage.getItem("token")}` },
});
const me = await res.json();
```
