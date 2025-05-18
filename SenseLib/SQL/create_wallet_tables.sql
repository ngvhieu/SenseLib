-- Script để tạo các bảng liên quan đến ví điện tử

-- Kiểm tra và tạo bảng Wallets nếu chưa tồn tại
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Wallets' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[Wallets] (
        [WalletID] INT IDENTITY(1,1) NOT NULL,
        [UserID] INT NOT NULL,
        [Balance] DECIMAL(18, 2) NOT NULL DEFAULT 0,
        [CreatedDate] DATETIME NOT NULL,
        [LastUpdatedDate] DATETIME NOT NULL,
        CONSTRAINT [PK_Wallets] PRIMARY KEY CLUSTERED ([WalletID] ASC),
        CONSTRAINT [FK_Wallets_Users] FOREIGN KEY ([UserID]) REFERENCES [dbo].[Users] ([UserID]) ON DELETE CASCADE
    );
    
    -- Tạo chỉ mục cho tìm kiếm nhanh
    CREATE UNIQUE INDEX [IX_Wallets_UserID] ON [dbo].[Wallets] ([UserID]);
    
    PRINT 'Đã tạo bảng Wallets';
END
ELSE
BEGIN
    PRINT 'Bảng Wallets đã tồn tại';
END

-- Kiểm tra và tạo bảng WalletTransactions nếu chưa tồn tại
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='WalletTransactions' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[WalletTransactions] (
        [TransactionID] INT IDENTITY(1,1) NOT NULL,
        [WalletID] INT NOT NULL,
        [Amount] DECIMAL(18, 2) NOT NULL,
        [TransactionDate] DATETIME NOT NULL,
        [Type] NVARCHAR(50) NOT NULL, -- "Credit" (nhận tiền), "Debit" (rút tiền)
        [Description] NVARCHAR(255) NULL,
        [DocumentID] INT NULL,
        [PurchaseID] INT NULL,
        CONSTRAINT [PK_WalletTransactions] PRIMARY KEY CLUSTERED ([TransactionID] ASC),
        CONSTRAINT [FK_WalletTransactions_Wallets] FOREIGN KEY ([WalletID]) REFERENCES [dbo].[Wallets] ([WalletID]) ON DELETE CASCADE,
        CONSTRAINT [FK_WalletTransactions_Documents] FOREIGN KEY ([DocumentID]) REFERENCES [dbo].[Documents] ([DocumentID]) ON DELETE SET NULL,
        CONSTRAINT [FK_WalletTransactions_Purchases] FOREIGN KEY ([PurchaseID]) REFERENCES [dbo].[Purchases] ([PurchaseID]) ON DELETE SET NULL
    );
    
    -- Tạo chỉ mục cho tìm kiếm nhanh
    CREATE INDEX [IX_WalletTransactions_WalletID] ON [dbo].[WalletTransactions] ([WalletID]);
    CREATE INDEX [IX_WalletTransactions_DocumentID] ON [dbo].[WalletTransactions] ([DocumentID]) WHERE [DocumentID] IS NOT NULL;
    CREATE INDEX [IX_WalletTransactions_PurchaseID] ON [dbo].[WalletTransactions] ([PurchaseID]) WHERE [PurchaseID] IS NOT NULL;
    
    PRINT 'Đã tạo bảng WalletTransactions';
END
ELSE
BEGIN
    PRINT 'Bảng WalletTransactions đã tồn tại';
END

-- Thêm cấu hình hệ thống cho phần trăm hoa hồng nếu chưa có
IF NOT EXISTS (SELECT * FROM [dbo].[SystemConfigs] WHERE [ConfigKey] = 'AuthorCommissionPercent')
BEGIN
    INSERT INTO [dbo].[SystemConfigs] ([ConfigKey], [ConfigValue], [Description])
    VALUES ('AuthorCommissionPercent', '80', N'Phần trăm hoa hồng cho tác giả khi bán tài liệu');
    
    PRINT 'Đã thêm cấu hình AuthorCommissionPercent';
END
ELSE
BEGIN
    PRINT 'Cấu hình AuthorCommissionPercent đã tồn tại';
END 