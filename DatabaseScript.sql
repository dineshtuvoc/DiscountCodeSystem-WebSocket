-- Below is the database script to use before starting project - mysql version 8.0.41


CREATE DATABASE IF NOT EXISTS discount_system;

USE discount_system;

CREATE TABLE IF NOT EXISTS DiscountCodes (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Code VARCHAR(8) NOT NULL,
    IsUsed BOOLEAN NOT NULL DEFAULT FALSE,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT UC_Code UNIQUE (Code)
);
