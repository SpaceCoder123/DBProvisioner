-- USERS TABLE (Expanded for GraphQL/gRPC practice)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        Id INT PRIMARY KEY IDENTITY,

        -- Basic Info
        FirstName NVARCHAR(100) NOT NULL,
        LastName NVARCHAR(100) NOT NULL,
        Email NVARCHAR(150) NOT NULL UNIQUE,
        PhoneNumber NVARCHAR(20),

        -- Profile Info
        DateOfBirth DATE NULL,
        Gender NVARCHAR(10) NULL,

        -- Address Info
        AddressLine1 NVARCHAR(200) NULL,
        AddressLine2 NVARCHAR(200) NULL,
        City NVARCHAR(100) NULL,
        State NVARCHAR(100) NULL,
        Country NVARCHAR(100) NULL,
        ZipCode NVARCHAR(20) NULL,

        -- Account Info
        IsActive BIT DEFAULT 1,
        IsDeleted BIT DEFAULT 0,

        -- Metadata
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        UpdatedAt DATETIME2 NULL
    )
END


-- ORDERS TABLE
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Orders')
BEGIN
    CREATE TABLE Orders (
        Id INT PRIMARY KEY IDENTITY,
        UserId INT NOT NULL,

        ProductName NVARCHAR(150) NOT NULL,
        Amount DECIMAL(10,2) NOT NULL,
        Quantity INT DEFAULT 1,

        Status NVARCHAR(50) DEFAULT 'Pending',

        OrderDate DATETIME2 DEFAULT GETDATE(),

        CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId)
        REFERENCES Users(Id)
        ON DELETE CASCADE
    )
END