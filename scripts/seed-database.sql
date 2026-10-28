/*
    Idempotent demo seed for EFCorePerformanceDemo.

    Apply the EF Core migration before running this script. The script inserts the same
    12 products, 20 customers, 100 orders, and 300 order items produced by DatabaseSeeder.
    It does nothing when any demo table already contains rows.

    sqlcmd -S localhost -E -C -d EFCorePerformanceDemo -i scripts/seed-database.sql
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Products', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Orders', N'U') IS NULL
   OR OBJECT_ID(N'dbo.OrderItems', N'U') IS NULL
BEGIN
    RAISERROR('Apply EF Core migrations before running scripts/seed-database.sql.', 16, 1);
    RETURN;
END;

IF EXISTS (SELECT 1 FROM dbo.Customers)
   OR EXISTS (SELECT 1 FROM dbo.Products)
   OR EXISTS (SELECT 1 FROM dbo.Orders)
   OR EXISTS (SELECT 1 FROM dbo.OrderItems)
BEGIN
    PRINT 'Seed skipped because demo tables already contain rows.';
    RETURN;
END;

BEGIN TRANSACTION;

DECLARE @productNumber int = 1;
WHILE @productNumber <= 12
BEGIN
    INSERT INTO dbo.Products (Sku, Name, Description, ListPrice, IsActive)
    VALUES (
        CONCAT('SKU-', RIGHT(CONCAT('000', @productNumber), 3)),
        CONCAT('Product ', RIGHT(CONCAT('00', @productNumber), 2)),
        CONCAT('Fictional catalog description for product ', RIGHT(CONCAT('00', @productNumber), 2), '. ', REPLICATE(N'd', 320)),
        CAST(10 + @productNumber AS decimal(18, 2)),
        CASE WHEN @productNumber % 7 = 0 THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END
    );

    SET @productNumber += 1;
END;

DECLARE @customerNumber int = 1;
WHILE @customerNumber <= 20
BEGIN
    INSERT INTO dbo.Customers (Name, Email, Notes, CreatedAt)
    VALUES (
        CONCAT('Customer ', RIGHT(CONCAT('00', @customerNumber), 2)),
        CONCAT('customer', RIGHT(CONCAT('00', @customerNumber), 2), '@example.test'),
        CONCAT('Fictional account notes for customer ', RIGHT(CONCAT('00', @customerNumber), 2), '. ', REPLICATE(N'n', 320)),
        DATEADD(day, @customerNumber, CAST('2023-01-01T00:00:00+00:00' AS datetimeoffset))
    );

    SET @customerNumber += 1;
END;

DECLARE @customer int = 1;
DECLARE @customerId int;
DECLARE @ordinal int;
DECLARE @sequence int;
DECLARE @orderId int;
DECLARE @itemNumber int;
DECLARE @total decimal(18, 2);
DECLARE @catalogNumber int;
DECLARE @productId int;
DECLARE @unitPrice decimal(18, 2);
DECLARE @quantity int;

WHILE @customer <= 20
BEGIN
    SET @customerId = (
        SELECT Id
        FROM dbo.Customers
        WHERE Email = CONCAT('customer', RIGHT(CONCAT('00', @customer), 2), '@example.test'));
    SET @ordinal = 1;

    WHILE @ordinal <= 5
    BEGIN
        SET @sequence = ((@customer - 1) * 5) + @ordinal;

        INSERT INTO dbo.Orders
        (
            CustomerId,
            OrderNumber,
            Status,
            PlacedAt,
            TotalAmount,
            ShippingAddress,
            InternalNotes
        )
        VALUES
        (
            @customerId,
            CONCAT('ORD-', RIGHT(CONCAT('0000', @sequence), 4)),
            (@sequence - 1) % 4,
            DATEADD(day, @sequence, CAST('2024-01-01T00:00:00+00:00' AS datetimeoffset)),
            CAST(0 AS decimal(18, 2)),
            CONCAT(100 + @sequence, ' Demo Street, Sample City'),
            CONCAT('Fictional internal order notes for sequence ', RIGHT(CONCAT('0000', @sequence), 4), '. ', REPLICATE(N'i', 320))
        );

        SET @orderId = CAST(SCOPE_IDENTITY() AS int);
        SET @itemNumber = 1;
        SET @total = 0;

        WHILE @itemNumber <= 3
        BEGIN
            SET @catalogNumber = ((@sequence + @itemNumber) % 12) + 1;
            SET @quantity = ((@sequence + @itemNumber) % 5) + 1;

            SELECT @productId = Id, @unitPrice = ListPrice
            FROM dbo.Products
            WHERE Sku = CONCAT('SKU-', RIGHT(CONCAT('000', @catalogNumber), 3));

            INSERT INTO dbo.OrderItems (OrderId, ProductId, Quantity, UnitPrice)
            VALUES (@orderId, @productId, @quantity, @unitPrice);

            SET @total += CAST(@quantity AS decimal(18, 2)) * @unitPrice;
            SET @itemNumber += 1;
        END;

        UPDATE dbo.Orders
        SET TotalAmount = @total
        WHERE Id = @orderId;

        SET @ordinal += 1;
    END;

    SET @customer += 1;
END;

COMMIT TRANSACTION;

PRINT 'Inserted 12 products, 20 customers, 100 orders, and 300 order items.';
