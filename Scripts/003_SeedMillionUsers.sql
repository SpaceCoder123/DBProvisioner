SET NOCOUNT ON;

DECLARE @TargetRows INT = 1000000;
DECLARE @ExistingRows INT;

SELECT @ExistingRows = COUNT(*)
FROM Users
WHERE Email LIKE 'loadtest.user.%@example.com';

IF @ExistingRows >= @TargetRows
BEGIN
    PRINT 'Load-test users already present. Skipping million-row seed.';
    RETURN;
END;

;WITH Numbers AS
(
    SELECT TOP (@TargetRows)
        ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS Number
    FROM sys.all_objects AS a
    CROSS JOIN sys.all_objects AS b
)
INSERT INTO Users
(
    FirstName,
    LastName,
    Email,
    PhoneNumber,
    DateOfBirth,
    Gender,
    AddressLine1,
    City,
    State,
    Country,
    ZipCode,
    IsActive,
    IsDeleted,
    CreatedAt
)
SELECT
    CONCAT('Load', Number),
    'TestUser',
    CONCAT('loadtest.user.', Number, '@example.com'),
    RIGHT(CONCAT('9000000000', Number), 10),
    DATEADD(DAY, -(Number % 12000), CAST('2000-01-01' AS DATE)),
    CASE WHEN Number % 2 = 0 THEN 'Male' ELSE 'Female' END,
    CONCAT('Street ', Number),
    CONCAT('City ', Number % 200),
    CONCAT('State ', Number % 30),
    'India',
    RIGHT(CONCAT('000000', Number % 1000000), 6),
    1,
    0,
    DATEADD(SECOND, Number % 86400, SYSUTCDATETIME())
FROM Numbers AS n
WHERE NOT EXISTS
(
    SELECT 1
    FROM Users AS u
    WHERE u.Email = CONCAT('loadtest.user.', n.Number, '@example.com')
);

PRINT 'Load-test user seed complete.';
