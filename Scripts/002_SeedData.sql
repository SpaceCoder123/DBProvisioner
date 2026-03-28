-- USERS
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'mohan@test.com')
BEGIN
    INSERT INTO Users (
        FirstName, LastName, Email, PhoneNumber,
        DateOfBirth, Gender,
        AddressLine1, City, State, Country, ZipCode
    )
    VALUES (
        'Mohan', 'Ram', 'mohan@test.com', '9999999999',
        '1998-05-10', 'Male',
        'Bangalore Main Road', 'Bangalore', 'Karnataka', 'India', '560001'
    )
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'rahul@test.com')
BEGIN
    INSERT INTO Users (
        FirstName, LastName, Email, PhoneNumber,
        DateOfBirth, Gender,
        AddressLine1, City, State, Country, ZipCode
    )
    VALUES (
        'Rahul', 'Sharma', 'rahul@test.com', '8888888888',
        '1995-08-15', 'Male',
        'MG Road', 'Delhi', 'Delhi', 'India', '110001'
    )
END


-- ORDERS
IF NOT EXISTS (SELECT 1 FROM Orders WHERE ProductName = 'Laptop')
BEGIN
    INSERT INTO Orders (UserId, ProductName, Amount, Quantity, Status)
    VALUES 
        (1, 'Laptop', 75000, 1, 'Completed'),
        (1, 'Mouse', 1500, 2, 'Completed'),
        (2, 'Keyboard', 3000, 1, 'Pending')
END