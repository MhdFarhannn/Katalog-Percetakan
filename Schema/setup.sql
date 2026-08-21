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


