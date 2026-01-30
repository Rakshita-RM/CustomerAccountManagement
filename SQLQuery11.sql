SELECT DB_NAME() AS CurrentDatabase;

-- See if the table exists in dbo schema
SELECT *
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 't_Accounts';

-- List all tables that look like Accounts
SELECT TABLE_SCHEMA, TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME LIKE '%Account%';