-- =============================================================
-- Katalog Percetakan - Migrasi Pricing Modes
--
-- Idempotent: aman dijalankan berulang. Setiap ADD COLUMN dicek
-- lewat information_schema lebih dulu (kompatibel MySQL & MariaDB).
--
-- Penggunaan:
--   mysql -u <user> -p katalog_percetakan < Schema/migration_pricing_modes.sql
--
-- Isi migrasi:
--   1. product.pricing_mode + product.dimension_unit
--      (default Fixed / meter => product lama tetap berperilaku
--       sama: harga_satuan + qty).
--   2. Ukuran_Produk.panjang_cm / lebar_cm (dimensi tetap varian)
--      dan Ukuran_Produk.harga (harga absolut varian, opsional).
--   3. Snapshot pricing pada Pesanan_Detail
--      (pricing_mode, width_m, height_m, length_m, dimension_unit,
--       subtotal) supaya harga pesanan lama tidak berubah.
--   4. Backfill data lama (aman, tidak menghapus apa pun).
-- =============================================================

-- =============================================================
-- 1. PRODUCT: PRICING MODE & SATUAN DIMENSI
-- =============================================================

SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'product' AND COLUMN_NAME = 'pricing_mode');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `product` ADD COLUMN `pricing_mode` VARCHAR(20) NOT NULL DEFAULT ''Fixed''',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'product' AND COLUMN_NAME = 'dimension_unit');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `product` ADD COLUMN `dimension_unit` VARCHAR(20) NOT NULL DEFAULT ''meter''',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

-- =============================================================
-- 2. UKURAN_PRODUK (VARIAN): DIMENSI TETAP & HARGA ABSOLUT
-- =============================================================

SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Ukuran_Produk' AND COLUMN_NAME = 'panjang_cm');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `Ukuran_Produk` ADD COLUMN `panjang_cm` INT NULL',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Ukuran_Produk' AND COLUMN_NAME = 'lebar_cm');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `Ukuran_Produk` ADD COLUMN `lebar_cm` INT NULL',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

-- NULL = pakai product.harga + harga_tambahan (perilaku lama).
SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Ukuran_Produk' AND COLUMN_NAME = 'harga');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `Ukuran_Produk` ADD COLUMN `harga` DECIMAL(15,2) NULL',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

-- =============================================================
-- 3. PESANAN_DETAIL: SNAPSHOT PRICING
--
-- Semua kolom NULLABLE supaya baris lama tetap valid. Nilai
-- baris lama di-backfill di langkah 4.
-- =============================================================

SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Pesanan_Detail' AND COLUMN_NAME = 'pricing_mode');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `Pesanan_Detail` ADD COLUMN `pricing_mode` VARCHAR(20) NULL',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Pesanan_Detail' AND COLUMN_NAME = 'width_m');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `Pesanan_Detail` ADD COLUMN `width_m` DECIMAL(12,4) NULL',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Pesanan_Detail' AND COLUMN_NAME = 'height_m');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `Pesanan_Detail` ADD COLUMN `height_m` DECIMAL(12,4) NULL',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Pesanan_Detail' AND COLUMN_NAME = 'length_m');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `Pesanan_Detail` ADD COLUMN `length_m` DECIMAL(12,4) NULL',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Pesanan_Detail' AND COLUMN_NAME = 'dimension_unit');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `Pesanan_Detail` ADD COLUMN `dimension_unit` VARCHAR(20) NULL',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

SET @exist := (SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Pesanan_Detail' AND COLUMN_NAME = 'subtotal');
SET @ddl := IF(@exist = 0,
    'ALTER TABLE `Pesanan_Detail` ADD COLUMN `subtotal` DECIMAL(15,2) NULL',
    'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

-- =============================================================
-- 4. BACKFILL DATA LAMA (tidak destruktif)
--
-- Baris Pesanan_Detail lama memakai perhitungan Fixed:
--   subtotal = harga_satuan x qty
-- sehingga snapshot-nya konsisten dengan aturan lama.
-- =============================================================

UPDATE `Pesanan_Detail`
SET `pricing_mode` = 'Fixed'
WHERE `pricing_mode` IS NULL;

UPDATE `Pesanan_Detail`
SET `dimension_unit` = 'meter'
WHERE `dimension_unit` IS NULL;

UPDATE `Pesanan_Detail`
SET `subtotal` = `harga_satuan` * `qty`
WHERE `subtotal` IS NULL;
