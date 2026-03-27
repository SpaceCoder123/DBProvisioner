IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'mohan@email.com')
BEGIN
    INSERT INTO Users (Name, Email)
    VALUES ('Mohan', 'mohan@email.com')
END