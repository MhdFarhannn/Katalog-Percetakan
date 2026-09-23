-- =============================================================
-- Katalog Percetakan - Database Schema
-- File ini disinkronkan dengan struktur database aktual.
-- Engine  : InnoDB
-- Charset : utf8mb4 / utf8mb4_unicode_ci
--
-- Audit & Soft Delete:
--   created_at / Created_At  : waktu baris dibuat
--   updated_at / Updated_At  : waktu baris terakhir diubah (otomatis)
--   deleted_at / Deleted_At  : waktu soft delete; NULL = baris aktif
-- Soft delete DIAPLIKASIKAN pada tabel: product, Alamat,
-- Pesanan, Pesanan_Detail. Tabel lain hanya punya kolom audit.
--
-- Urutan pembuatan tabel mengikuti dependency foreign key.
-- Jalankan dari atas ke bawah.
-- Untuk database yang sudah berjalan, pakai
-- Schema/migration_soft_delete_audit.sql (idempotent).
-- =============================================================

-- =============================================================
-- Accountability
-- =============================================================

-- Roles
CREATE TABLE IF NOT EXISTS Roles (
    Id INT NOT NULL AUTO_INCREMENT,
    Nama VARCHAR(255) NOT NULL,
    created_at TIMESTAMP NULL DEFAULT current_timestamp(),
    updated_at TIMESTAMP NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
    PRIMARY KEY (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- User
CREATE TABLE IF NOT EXISTS User (
    Id INT NOT NULL AUTO_INCREMENT,
    Nama VARCHAR(255) NOT NULL,
    Id_Role INT NULL,
    Email VARCHAR(255) NULL,
    Password VARCHAR(255) NULL,
    Auth_Provider VARCHAR(50) NOT NULL DEFAULT 'local',
    External_Id VARCHAR(255) NULL,
    Refresh_Token VARCHAR(255) NULL,
    Refresh_Token_Expired TIMESTAMP NULL DEFAULT NULL,
    Is_Active TINYINT(1) DEFAULT 1,
    Created_At TIMESTAMP NULL DEFAULT current_timestamp(),
    Updated_At TIMESTAMP NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
    PRIMARY KEY (Id),
    UNIQUE KEY uq_provider_external (Auth_Provider, External_Id),
    KEY fk_user_role (Id_Role),
    CONSTRAINT fk_user_role FOREIGN KEY (Id_Role) REFERENCES Roles(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================================
-- Master Product
-- =============================================================

-- kategory_product
CREATE TABLE IF NOT EXISTS kategory_product (
    id INT AUTO_INCREMENT,
    nama VARCHAR(100) NOT NULL,
    created_at TIMESTAMP NULL DEFAULT current_timestamp(),
    updated_at TIMESTAMP NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
    PRIMARY KEY (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- status_product
CREATE TABLE IF NOT EXISTS status_product (
    id INT AUTO_INCREMENT,
    nama VARCHAR(100) NOT NULL,
    created_at TIMESTAMP NULL DEFAULT current_timestamp(),
    updated_at TIMESTAMP NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
    PRIMARY KEY (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- status_pengerjaan
CREATE TABLE IF NOT EXISTS status_pengerjaan (
    id INT AUTO_INCREMENT,
    nama VARCHAR(100) NOT NULL,
    created_at TIMESTAMP NULL DEFAULT current_timestamp(),
    updated_at TIMESTAMP NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
    PRIMARY KEY (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- status_payment
CREATE TABLE IF NOT EXISTS status_payment (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nama VARCHAR(100) NOT NULL,
    created_at TIMESTAMP NULL DEFAULT current_timestamp(),
    updated_at TIMESTAMP NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- product
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
        FOREIGN KEY (idKategoriProduct)
        REFERENCES kategory_product(id)
        ON UPDATE CASCADE,
    CONSTRAINT fk_status_product
        FOREIGN KEY (idStatusProduct)
        REFERENCES status_product(id)
        ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Ukuran_Produk
CREATE TABLE IF NOT EXISTS Ukuran_Produk (
    id INT AUTO_INCREMENT,
    idProduct INT NOT NULL,
    nama VARCHAR(50) NOT NULL,
    harga_tambahan DECIMAL(15,2) DEFAULT 0.00,
    created_at TIMESTAMP NULL DEFAULT current_timestamp(),
    updated_at TIMESTAMP NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
    PRIMARY KEY (id),
    KEY fk_ukuran_product (idProduct),
    CONSTRAINT fk_ukuran_product
        FOREIGN KEY (idProduct)
        REFERENCES product(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================================
-- Customer / Pelanggan
-- =============================================================

-- Alamat
CREATE TABLE IF NOT EXISTS Alamat (
    id INT AUTO_INCREMENT,
    idUser INT NOT NULL,
    content TEXT NOT NULL,
    no_telepon VARCHAR(20) NULL,
    created_at TIMESTAMP NULL DEFAULT current_timestamp(),
    updated_at TIMESTAMP NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
    deleted_at TIMESTAMP NULL DEFAULT NULL,
    PRIMARY KEY (id),
    KEY fk_alamat_user (idUser),
    KEY idx_alamat_aktif (idUser, deleted_at),
    CONSTRAINT fk_alamat_user
        FOREIGN KEY (idUser)
        REFERENCES User(Id)
        ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Layanan
CREATE TABLE IF NOT EXISTS Layanan (
    id INT AUTO_INCREMENT,
    nama VARCHAR(100) NOT NULL,
    deskripsi TEXT NULL,
    imagePath VARCHAR(255) NULL,
    background_color VARCHAR(25) NULL,
    created_at TIMESTAMP NULL DEFAULT current_timestamp(),
    updated_at TIMESTAMP NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
    PRIMARY KEY (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================================
-- Order / Pesanan
-- =============================================================

-- Pesanan
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
    KEY idx_pesanan_tanggal (Created_At, Deleted_At),
    CONSTRAINT fk_pesanan_user
        FOREIGN KEY (idUser)
        REFERENCES User(Id),
    CONSTRAINT fk_pesanan_alamat
        FOREIGN KEY (idAlamat)
        REFERENCES Alamat(id),
    CONSTRAINT fk_pesanan_status
        FOREIGN KEY (idStatusPengerjaan)
        REFERENCES status_pengerjaan(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Pesanan_Detail
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
    CONSTRAINT fk_detail_pesanan
        FOREIGN KEY (idPesanan)
        REFERENCES Pesanan(id),
    CONSTRAINT fk_detail_product
        FOREIGN KEY (idProduct)
        REFERENCES product(id),
    CONSTRAINT fk_detail_ukuran
        FOREIGN KEY (idUkuranProduk)
        REFERENCES Ukuran_Produk(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================================
-- Payment (Midtrans)
-- =============================================================

-- payments (bukti transaksi: tanpa soft delete)
CREATE TABLE IF NOT EXISTS payments (
    id INT AUTO_INCREMENT,
    idPesanan INT NOT NULL,
    idStatusPayment INT NOT NULL,
    midtrans_order_id VARCHAR(100) NOT NULL,
    midtrans_transaction_id VARCHAR(100) NULL,
    snap_token VARCHAR(255) NULL,
    payment_type VARCHAR(50) NULL,
    gross_amount DECIMAL(15,2) NOT NULL,
    transaction_status VARCHAR(50) NULL,
    transaction_time DATETIME NULL,
    settlement_time DATETIME NULL,
    expiry_time DATETIME NULL,
    created_at TIMESTAMP NULL DEFAULT current_timestamp(),
    updated_at TIMESTAMP NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
    PRIMARY KEY (id),
    UNIQUE KEY uq_payments_midtrans_order (midtrans_order_id),
    KEY fk_payments_pesanan (idPesanan),
    KEY fk_payments_status (idStatusPayment),
    KEY idx_payments_status (idPesanan, idStatusPayment),
    CONSTRAINT fk_payments_pesanan
        FOREIGN KEY (idPesanan)
        REFERENCES Pesanan(id)
        ON UPDATE CASCADE
        ON DELETE CASCADE,
    CONSTRAINT fk_payments_status
        FOREIGN KEY (idStatusPayment)
        REFERENCES status_payment(id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================================
-- Seed Data (reference)
-- =============================================================

INSERT IGNORE INTO Roles (Id, Nama) VALUES
    (1, 'Admin'),
    (2, 'Petugas'),
    (3, 'Pelanggan');

INSERT IGNORE INTO status_product (id, nama) VALUES
    (2, 'Tersedia'),
    (3, 'Habis');

INSERT IGNORE INTO status_pengerjaan (id, nama) VALUES
    (1, 'Sedang Berlangsung'),
    (2, 'Dibatalkan'),
    (3, 'Selesai');

INSERT IGNORE INTO kategory_product (id, nama) VALUES
    (2, 'Percetakan'),
    (3, 'Packaging'),
    (4, 'Merchandise');

INSERT IGNORE INTO status_payment (id, nama) VALUES
    (1, 'MENUNGGU PEMBAYARAN'),
    (2, 'DIBAYAR'),
    (3, 'DIBATALKAN'),
    (4, 'KEDALUWARSA'),
    (5, 'GAGAL'),
    (6, 'DIKEMBALIKAN');
