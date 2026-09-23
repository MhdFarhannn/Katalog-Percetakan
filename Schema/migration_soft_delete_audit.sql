-- =============================================================
-- Katalog Percetakan - Migrasi Audit & Soft Delete
--
-- Idempotent: aman dijalankan berulang. Setiap ADD COLUMN /
-- ADD INDEX dicek lewat information_schema lebih dulu, sehingga
-- kompatibel dengan MySQL maupun MariaDB.
--
-- Penggunaan:
--   mysql -u <user> -p katalog_percetakan < Schema/migration_soft_delete_audit.sql
--
-- Isi migrasi:
--   1. Kolom audit  (created_at / Created_At, updated_at / Updated_At)
--      pada semua tabel yang belum punya.
--   2. Kolom soft delete (deleted_at / Deleted_At) pada tabel
--      product, Alamat, Pesanan, Pesanan_Detail.
--   3. Index pendukung filter soft delete & laporan tanggal.
--   4. Seed status_pengerjaan (3, 'Selesai') untuk endpoint
--      PUT /api/v1/pesanan/{id}/status.
-- =============================================================

-- -------------------------------------------------------------
-- Helper pattern (dipakai berulang di bawah):
--   SET @exist := cek information_schema
--   SET @ddl   := ALTER bila kolom/index belum ada, ELSE SELECT 1
--   PREPARE / EXECUTE / DEALLOCATE
-- -------------------------------------------------------------

-- =============================================================
-- 1. KOLOM AUDIT
-- =============================================================

-- Roles
SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Roles' AND COLUMN_NAME = 'created_at');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `Roles` ADD COLUMN `created_at` TIMESTAMP NULL DEFAULT current_timestamp()',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Roles' AND COLUMN_NAME = 'updated_at');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `Roles` ADD COLUMN `updated_at` TIMESTAMP NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

-- kategory_product
SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'kategory_product' AND COLUMN_NAME = 'created_at');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `kategory_product` ADD COLUMN `created_at` TIMESTAMP NULL DEFAULT current_timestamp()',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'kategory_product' AND COLUMN_NAME = 'updated_at');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `kategory_product` ADD COLUMN `updated_at` TIMESTAMP NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

-- status_product
SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'status_product' AND COLUMN_NAME = 'created_at');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `status_product` ADD COLUMN `created_at` TIMESTAMP NULL DEFAULT current_timestamp()',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'status_product' AND COLUMN_NAME = 'updated_at');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `status_product` ADD COLUMN `updated_at` TIMESTAMP NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

-- status_pengerjaan
SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'status_pengerjaan' AND COLUMN_NAME = 'created_at');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `status_pengerjaan` ADD COLUMN `created_at` TIMESTAMP NULL DEFAULT current_timestamp()',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'status_pengerjaan' AND COLUMN_NAME = 'updated_at');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `status_pengerjaan` ADD COLUMN `updated_at` TIMESTAMP NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

-- status_payment
SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'status_payment' AND COLUMN_NAME = 'created_at');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `status_payment` ADD COLUMN `created_at` TIMESTAMP NULL DEFAULT current_timestamp()',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'status_payment' AND COLUMN_NAME = 'updated_at');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `status_payment` ADD COLUMN `updated_at` TIMESTAMP NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

-- Ukuran_Produk
SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Ukuran_Produk' AND COLUMN_NAME = 'created_at');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `Ukuran_Produk` ADD COLUMN `created_at` TIMESTAMP NULL DEFAULT current_timestamp()',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Ukuran_Produk' AND COLUMN_NAME = 'updated_at');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `Ukuran_Produk` ADD COLUMN `updated_at` TIMESTAMP NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

-- Layanan
SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Layanan' AND COLUMN_NAME = 'created_at');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `Layanan` ADD COLUMN `created_at` TIMESTAMP NULL DEFAULT current_timestamp()',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Layanan' AND COLUMN_NAME = 'updated_at');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `Layanan` ADD COLUMN `updated_at` TIMESTAMP NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

-- product (audit + soft delete)
SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'product' AND COLUMN_NAME = 'created_at');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `product` ADD COLUMN `created_at` TIMESTAMP NULL DEFAULT current_timestamp()',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'product' AND COLUMN_NAME = 'updated_at');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `product` ADD COLUMN `updated_at` TIMESTAMP NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'product' AND COLUMN_NAME = 'deleted_at');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `product` ADD COLUMN `deleted_at` TIMESTAMP NULL DEFAULT NULL',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

-- Alamat (audit + soft delete)
SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Alamat' AND COLUMN_NAME = 'created_at');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `Alamat` ADD COLUMN `created_at` TIMESTAMP NULL DEFAULT current_timestamp()',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Alamat' AND COLUMN_NAME = 'updated_at');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `Alamat` ADD COLUMN `updated_at` TIMESTAMP NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Alamat' AND COLUMN_NAME = 'deleted_at');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `Alamat` ADD COLUMN `deleted_at` TIMESTAMP NULL DEFAULT NULL',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

-- Pesanan (audit + soft delete)
SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Pesanan' AND COLUMN_NAME = 'Updated_At');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `Pesanan` ADD COLUMN `Updated_At` TIMESTAMP NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Pesanan' AND COLUMN_NAME = 'Deleted_At');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `Pesanan` ADD COLUMN `Deleted_At` TIMESTAMP NULL DEFAULT NULL',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

-- Pesanan_Detail (audit + soft delete)
SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Pesanan_Detail' AND COLUMN_NAME = 'created_at');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `Pesanan_Detail` ADD COLUMN `created_at` TIMESTAMP NULL DEFAULT current_timestamp()',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Pesanan_Detail' AND COLUMN_NAME = 'updated_at');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `Pesanan_Detail` ADD COLUMN `updated_at` TIMESTAMP NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Pesanan_Detail' AND COLUMN_NAME = 'deleted_at');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `Pesanan_Detail` ADD COLUMN `deleted_at` TIMESTAMP NULL DEFAULT NULL',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

-- =============================================================
-- 2. INDEX PENDUKUNG
-- =============================================================

-- product
SET @exist := (SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'product' AND INDEX_NAME = 'idx_product_deleted');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `product` ADD KEY `idx_product_deleted` (`deleted_at`)',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

-- Alamat
SET @exist := (SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Alamat' AND INDEX_NAME = 'idx_alamat_aktif');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `Alamat` ADD KEY `idx_alamat_aktif` (`idUser`, `deleted_at`)',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

-- Pesanan
SET @exist := (SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Pesanan' AND INDEX_NAME = 'idx_pesanan_aktif');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `Pesanan` ADD KEY `idx_pesanan_aktif` (`idUser`, `Deleted_At`)',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

SET @exist := (SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Pesanan' AND INDEX_NAME = 'idx_pesanan_status');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `Pesanan` ADD KEY `idx_pesanan_status` (`idStatusPengerjaan`, `Deleted_At`)',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

SET @exist := (SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Pesanan' AND INDEX_NAME = 'idx_pesanan_tanggal');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `Pesanan` ADD KEY `idx_pesanan_tanggal` (`Created_At`, `Deleted_At`)',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

-- Pesanan_Detail
SET @exist := (SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Pesanan_Detail' AND INDEX_NAME = 'idx_detail_aktif');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `Pesanan_Detail` ADD KEY `idx_detail_aktif` (`idPesanan`, `deleted_at`)',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

-- payments (pencarian status pembayaran per pesanan)
SET @exist := (SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'payments' AND INDEX_NAME = 'idx_payments_status');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `payments` ADD KEY `idx_payments_status` (`idPesanan`, `idStatusPayment`)',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

-- =============================================================
-- 3. SEED STATUS PENGERJAAN
-- =============================================================
-- Dipakai endpoint PUT /api/v1/pesanan/{id}/status (Admin).
INSERT IGNORE INTO status_pengerjaan (id, nama) VALUES
    (1, 'Sedang Berlangsung'),
    (2, 'Dibatalkan'),
    (3, 'Selesai');

-- =============================================================
-- 4. SEED STATUS PAYMENT (bila tabelnya baru dibuat kosong)
-- =============================================================
INSERT IGNORE INTO status_payment (id, nama) VALUES
    (1, 'MENUNGGU PEMBAYARAN'),
    (2, 'DIBAYAR'),
    (3, 'DIBATALKAN'),
    (4, 'KEDALUWARSA'),
    (5, 'GAGAL'),
    (6, 'DIKEMBALIKAN');
