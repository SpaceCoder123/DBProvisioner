IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserExportJobs')
BEGIN
    CREATE TABLE UserExportJobs (
        JobId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        Format NVARCHAR(20) NOT NULL,
        Status NVARCHAR(20) NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL,
        StartedAtUtc DATETIME2 NULL,
        CompletedAtUtc DATETIME2 NULL,
        FilePath NVARCHAR(500) NULL,
        FileName NVARCHAR(260) NULL,
        Error NVARCHAR(MAX) NULL
    );

    CREATE INDEX IX_UserExportJobs_Status_CreatedAtUtc
        ON UserExportJobs (Status, CreatedAtUtc DESC);
END
