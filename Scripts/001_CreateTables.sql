IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        Id INT PRIMARY KEY IDENTITY,
        Name NVARCHAR(100),
        Email NVARCHAR(100)
    )
END