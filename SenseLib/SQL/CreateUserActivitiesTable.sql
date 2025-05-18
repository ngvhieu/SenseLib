-- Tạo bảng UserActivities 
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserActivities')
BEGIN
    CREATE TABLE UserActivities (
        ActivityID INT IDENTITY(1,1) PRIMARY KEY,
        UserID INT NOT NULL,
        ActivityDate DATETIME NOT NULL DEFAULT GETDATE(),
        ActivityType NVARCHAR(50) NOT NULL,
        DocumentID INT NULL,
        CommentID INT NULL,
        Description NVARCHAR(500) NULL,
        AdditionalData NVARCHAR(1000) NULL,
        CONSTRAINT FK_UserActivities_Users FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE,
        CONSTRAINT FK_UserActivities_Documents FOREIGN KEY (DocumentID) REFERENCES Documents(DocumentID) ON DELETE SET NULL
    );
    
    -- Tạo các chỉ mục để tối ưu truy vấn
    CREATE INDEX IX_UserActivities_UserID ON UserActivities(UserID);
    CREATE INDEX IX_UserActivities_ActivityDate ON UserActivities(ActivityDate);
    CREATE INDEX IX_UserActivities_ActivityType ON UserActivities(ActivityType);
    CREATE INDEX IX_UserActivities_DocumentID ON UserActivities(DocumentID);
    
    PRINT 'Đã tạo bảng UserActivities';
END
ELSE
BEGIN
    PRINT 'Bảng UserActivities đã tồn tại';
END 