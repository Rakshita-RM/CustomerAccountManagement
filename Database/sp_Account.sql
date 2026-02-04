/****** Stored procedure: sp_Account ******/
SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.sp_Account', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Account;
GO

CREATE PROCEDURE dbo.sp_Account
    @Action NVARCHAR(16),                       -- 'Create', 'Update', 'Delete', 'GetById', 'GetAll'
    @AccountID INT = NULL OUTPUT,               -- used for Update/Delete/GetById and returned after Create
    @Branch NVARCHAR(100) = NULL,
    @CustomerName NVARCHAR(100) = NULL,
    @CustomerID NVARCHAR(50) = NULL,
    @AccountType NVARCHAR(50) = NULL,
    @Balance DECIMAL(18,2) = NULL,
    @Status NVARCHAR(20) = NULL,
    @CreatedDate DATETIME2(7) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF @Action = 'Create'
        BEGIN
            -- provide defaults if caller omitted them
            IF @Branch IS NULL SET @Branch = N'Main Branch';
            IF @AccountType IS NULL SET @AccountType = N'Savings';
            IF @Balance IS NULL SET @Balance = 0.00;
            IF @Status IS NULL SET @Status = N'Active';
            IF @CreatedDate IS NULL SET @CreatedDate = SYSUTCDATETIME();

            INSERT INTO dbo.t_Account
                (Branch, CustomerName, CustomerID, AccountType, Balance, Status, CreatedDate)
            VALUES
                (@Branch, @CustomerName, @CustomerID, @AccountType, @Balance, @Status, @CreatedDate);

            SET @AccountID = CAST(SCOPE_IDENTITY() AS INT);

            -- return the newly created row (convenient for API)
            SELECT *
            FROM dbo.t_Account
            WHERE AccountID = @AccountID;
        END
        ELSE IF @Action = 'Update'
        BEGIN
            IF @AccountID IS NULL
                THROW 50000, 'AccountID is required for Update action.', 1;

            UPDATE dbo.t_Account
            SET
                Branch = COALESCE(@Branch, Branch),
                CustomerName = COALESCE(@CustomerName, CustomerName),
                CustomerID = COALESCE(@CustomerID, CustomerID),
                AccountType = COALESCE(@AccountType, AccountType),
                Balance = COALESCE(@Balance, Balance),
                Status = COALESCE(@Status, Status)
            WHERE AccountID = @AccountID;

            SELECT @@ROWCOUNT AS AffectedRows;

            -- optionally return updated row
            SELECT *
            FROM dbo.t_Account
            WHERE AccountID = @AccountID;
        END
        ELSE IF @Action = 'Delete'
        BEGIN
            IF @AccountID IS NULL
                THROW 50001, 'AccountID is required for Delete action.', 1;

            DELETE FROM dbo.t_Account
            WHERE AccountID = @AccountID;

            SELECT @@ROWCOUNT AS AffectedRows;
        END
        ELSE IF @Action = 'GetById'
        BEGIN
            IF @AccountID IS NULL
                THROW 50002, 'AccountID is required for GetById action.', 1;

            SELECT *
            FROM dbo.t_Account
            WHERE AccountID = @AccountID;
        END
        ELSE IF @Action = 'GetAll'
        BEGIN
            SELECT *
            FROM dbo.t_Account
            ORDER BY AccountID;
        END
        ELSE
        BEGIN
            THROW 50003, 'Invalid Action specified. Valid values: Create, Update, Delete, GetById, GetAll.', 1;
        END
    END TRY
    BEGIN CATCH
        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrState INT = ERROR_STATE();

        -- rethrow to caller
        RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
    END CATCH
END
GO