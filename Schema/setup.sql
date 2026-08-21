//Accountability

///Role\\\
CREATE TABLE IF NOT EXISTS Roles(
  Id INT NOT NULL PRIMARY KEY AUTO_INCREMENT,
  Nama VARCHAR(255) NOT NULL
);

INSERT INTO Roles(Nama) VALUES('Admin'),('Petugas'),('Pelanggan');

///User\\\
CREATE TABLE IF NOT EXISTS User(
  Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  Nama VARCHAR(255) NOT NULL,
  Id_Role INT,
  Email VARCHAR(255) NULL,
  Password VARCHAR(255) NULL,
  Auth_Provider VARCHAR(50) NOT NULL DEFAULT 'local',
  External_Id VARCHAR(255) NULL,
  Refresh_Token VARCHAR(255) DEFAULT NULL,
  Refresh_Token_Expired TIMESTAMP NULL DEFAULT NULL,
  Is_Active tinyint(1) DEFAULT 1,
  Created_At TIMESTAMP NULL DEFAULT current_timestamp(),
  Updated_At TIMESTAMP NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  CONSTRAINT fk_user_role FOREIGN KEY(Id_Role) REFERENCES Roles(Id),
  CONSTRAINT uq_provider_external UNIQUE (Auth_Provider, External_Id)
);

CREATE TABLE kategory_product (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nama VARCHAR(100) NOT NULL
);

CREATE TABLE product (
    id INT AUTO_INCREMENT PRIMARY KEY,
    idKategoriProduct INT NOT NULL,
    idStatusProduct INT NOT NULL,
    nama VARCHAR(150) NOT NULL,
    deskripsi TEXT,
    imagePath VARCHAR(255),
    harga DECIMAL(15,2) NOT NULL,

    CONSTRAINT fk_product_kategory
        FOREIGN KEY (idKategoriProduct)
        REFERENCES kategory_product(id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT
    CONSTRAINT fk_status_product
        FOREIGN KEY (idStatusProduct)
        REFERENCES status_product(id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT
);

CREATE TABLE status_product (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nama VARCHAR(100) NOT NULL
);