using System;
using System.Configuration;
using System.Data.SqlClient;

namespace CyberPulseAdministration.DataAccess
{
    public static class DatabaseInitializer
    {
        private static readonly string ConnectionString =
            ConfigurationManager.ConnectionStrings["CyberPulseDb"]?.ConnectionString;

        public static void InitializeDatabase()
        {
            if (string.IsNullOrEmpty(ConnectionString))
            {
                System.Diagnostics.Debug.WriteLine("DatabaseInitializer: Connection string CyberPulseDb is missing.");
                return;
            }

            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();

                    // 1. Create table if not exists
                    string createTableQuery = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[HrFiles]') AND type in (N'U'))
                        BEGIN
                            CREATE TABLE [dbo].[HrFiles](
                                [ID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                [Title] [nvarchar](100) NOT NULL,
                                [FileName] [nvarchar](255) NOT NULL,
                                [FilePath] [nvarchar](512) NOT NULL,
                                [FileType] [nvarchar](50) NOT NULL,
                                [UploadDate] [datetime] NOT NULL DEFAULT (getdate()),
                                [FileSize] [bigint] NOT NULL
                            )
                        END";
                    using (var cmd = new SqlCommand(createTableQuery, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 2. Create usp_GetHrFiles if not exists
                    string createSpGetQuery = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetHrFiles]') AND type in (N'P', N'PC'))
                        BEGIN
                            EXEC dbo.sp_executesql @statement = N'
                            CREATE PROCEDURE [dbo].[usp_GetHrFiles]
                            AS
                            BEGIN
                                SELECT ID, Title, FileName, FilePath, FileType, UploadDate, FileSize
                                FROM HrFiles
                                ORDER BY UploadDate DESC
                            END'
                        END";
                    using (var cmd = new SqlCommand(createSpGetQuery, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 3. Create usp_InsertHrFile if not exists
                    string createSpInsertQuery = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_InsertHrFile]') AND type in (N'P', N'PC'))
                        BEGIN
                            EXEC dbo.sp_executesql @statement = N'
                            CREATE PROCEDURE [dbo].[usp_InsertHrFile]
                                @Title NVARCHAR(100),
                                @FileName NVARCHAR(255),
                                @FilePath NVARCHAR(512),
                                @FileType NVARCHAR(50),
                                @FileSize BIGINT
                            AS
                            BEGIN
                                INSERT INTO HrFiles (Title, FileName, FilePath, FileType, FileSize, UploadDate)
                                VALUES (@Title, @FileName, @FilePath, @FileType, @FileSize, GETDATE());
                                SELECT SCOPE_IDENTITY();
                            END'
                        END";
                    using (var cmd = new SqlCommand(createSpInsertQuery, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 4. Create usp_DeleteHrFile if not exists
                    string createSpDeleteQuery = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_DeleteHrFile]') AND type in (N'P', N'PC'))
                        BEGIN
                            EXEC dbo.sp_executesql @statement = N'
                            CREATE PROCEDURE [dbo].[usp_DeleteHrFile]
                                @ID INT
                            AS
                            BEGIN
                                DELETE FROM HrFiles WHERE ID = @ID;
                            END'
                        END";
                    using (var cmd = new SqlCommand(createSpDeleteQuery, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 4b. Create usp_GetHrFileById if not exists
                    string createSpGetHrFileById = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetHrFileById]') AND type in (N'P', N'PC'))
                        BEGIN
                            EXEC dbo.sp_executesql @statement = N'
                            CREATE PROCEDURE [dbo].[usp_GetHrFileById]
                                @ID INT
                            AS
                            BEGIN
                                SELECT ID, Title, FileName, FilePath, FileType, UploadDate, FileSize
                                FROM HrFiles
                                WHERE ID = @ID;
                            END'
                        END";
                    using (var cmd = new SqlCommand(createSpGetHrFileById, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // ===== NewsArticle Table & Stored Procedures =====

                    // 5. Create NewsArticle table if not exists
                    string createNewsArticleTable = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[NewsArticle]') AND type in (N'U'))
                        BEGIN
                            CREATE TABLE [dbo].[NewsArticle](
                                [ID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                [ImageName] [nvarchar](255) NULL,
                                [ImagePath] [nvarchar](255) NULL,
                                [Type] [nvarchar](10) NOT NULL,
                                [Description] [nvarchar](1024) NULL,
                                [Date] [date] NOT NULL,
                                [URL] [nvarchar](1024) NOT NULL,
                                [ISactive] [bit] NULL DEFAULT(1)
                            )
                        END";
                    using (var cmd = new SqlCommand(createNewsArticleTable, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 6. Create usp_GetNewsArticles
                    string createSpGetNewsArticles = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetNewsArticles]') AND type in (N'P', N'PC'))
                        BEGIN
                            EXEC dbo.sp_executesql @statement = N'
                            CREATE PROCEDURE [dbo].[usp_GetNewsArticles]
                            AS
                            BEGIN
                                SELECT ID, ImageName, ImagePath, Type, Description, Date, URL, ISactive
                                FROM NewsArticle
                                ORDER BY Date DESC
                            END'
                        END";
                    using (var cmd = new SqlCommand(createSpGetNewsArticles, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 7. Create usp_GetNewsArticleById
                    string createSpGetNewsArticleById = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetNewsArticleById]') AND type in (N'P', N'PC'))
                        BEGIN
                            EXEC dbo.sp_executesql @statement = N'
                            CREATE PROCEDURE [dbo].[usp_GetNewsArticleById]
                                @ID INT
                            AS
                            BEGIN
                                SELECT ID, ImageName, ImagePath, Type, Description, Date, URL, ISactive
                                FROM NewsArticle
                                WHERE ID = @ID
                            END'
                        END";
                    using (var cmd = new SqlCommand(createSpGetNewsArticleById, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 8. Create usp_InsertNewsArticle
                    string createSpInsertNewsArticle = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_InsertNewsArticle]') AND type in (N'P', N'PC'))
                        BEGIN
                            EXEC dbo.sp_executesql @statement = N'
                            CREATE PROCEDURE [dbo].[usp_InsertNewsArticle]
                                @ImageName NVARCHAR(255) = NULL,
                                @ImagePath NVARCHAR(255) = NULL,
                                @Type NVARCHAR(10),
                                @Description NVARCHAR(1024) = NULL,
                                @Date DATE,
                                @URL NVARCHAR(1024),
                                @IsActive BIT = 1
                            AS
                            BEGIN
                                INSERT INTO NewsArticle (ImageName, ImagePath, Type, Description, Date, URL, ISactive)
                                VALUES (@ImageName, @ImagePath, @Type, @Description, @Date, @URL, @IsActive);
                                SELECT SCOPE_IDENTITY();
                            END'
                        END";
                    using (var cmd = new SqlCommand(createSpInsertNewsArticle, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 9. Create usp_UpdateNewsArticle
                    string createSpUpdateNewsArticle = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_UpdateNewsArticle]') AND type in (N'P', N'PC'))
                        BEGIN
                            EXEC dbo.sp_executesql @statement = N'
                            CREATE PROCEDURE [dbo].[usp_UpdateNewsArticle]
                                @ID INT,
                                @ImageName NVARCHAR(255) = NULL,
                                @ImagePath NVARCHAR(255) = NULL,
                                @Type NVARCHAR(10),
                                @Description NVARCHAR(1024) = NULL,
                                @Date DATE,
                                @URL NVARCHAR(1024),
                                @IsActive BIT
                            AS
                            BEGIN
                                UPDATE NewsArticle SET
                                    ImageName = @ImageName,
                                    ImagePath = @ImagePath,
                                    Type = @Type,
                                    Description = @Description,
                                    Date = @Date,
                                    URL = @URL,
                                    ISactive = @IsActive
                                WHERE ID = @ID
                            END'
                        END";
                    using (var cmd = new SqlCommand(createSpUpdateNewsArticle, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 10. Create usp_DeleteNewsArticle
                    string createSpDeleteNewsArticle = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_DeleteNewsArticle]') AND type in (N'P', N'PC'))
                        BEGIN
                            EXEC dbo.sp_executesql @statement = N'
                            CREATE PROCEDURE [dbo].[usp_DeleteNewsArticle]
                                @ID INT
                            AS
                            BEGIN
                                DELETE FROM NewsArticle WHERE ID = @ID;
                            END'
                        END";
                    using (var cmd = new SqlCommand(createSpDeleteNewsArticle, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 11. Create usp_GetActiveNewsArticles
                    string createSpGetActiveNewsArticles = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetActiveNewsArticles]') AND type in (N'P', N'PC'))
                        BEGIN
                            EXEC dbo.sp_executesql @statement = N'
                            CREATE PROCEDURE [dbo].[usp_GetActiveNewsArticles]
                            AS
                            BEGIN
                                SELECT ID, ImageName, ImagePath, Type, Description, Date, URL, ISactive
                                FROM NewsArticle
                                WHERE ISactive = 1
                                ORDER BY Date DESC
                            END'
                        END";
                    using (var cmd = new SqlCommand(createSpGetActiveNewsArticles, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 12. Create usp_ToggleNewsArticleActive
                    string createSpToggleNewsArticleActive = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_ToggleNewsArticleActive]') AND type in (N'P', N'PC'))
                        BEGIN
                            EXEC dbo.sp_executesql @statement = N'
                            CREATE PROCEDURE [dbo].[usp_ToggleNewsArticleActive]
                                @ID INT,
                                @IsActive BIT
                            AS
                            BEGIN
                                UPDATE NewsArticle SET ISactive = @IsActive WHERE ID = @ID;
                            END'
                        END";
                    using (var cmd = new SqlCommand(createSpToggleNewsArticleActive, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // ===== Announcement Table & Stored Procedures =====
                    
                    // 13. Create Announcement table if not exists
                    string createAnnouncementTable = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Announcement]') AND type in (N'U'))
                        BEGIN
                            CREATE TABLE [dbo].[Announcement](
                                [ID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                [Title] [nvarchar](50) NOT NULL,
                                [Description] [nvarchar](max) NOT NULL,
                                [Date] [datetime] NOT NULL,
                                [ShortDescription] [nvarchar](500) NOT NULL,
                                [PageTitle] [nvarchar](200) NOT NULL,
                                [IsActive] [bit] NOT NULL,
                                [AnnouncementGuid] [uniqueidentifier] NULL
                            )
                        END";
                    using (var cmd = new SqlCommand(createAnnouncementTable, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 14. Create usp_GetAnnouncements
                    string createSpGetAnnouncements = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetAnnouncements]') AND type in (N'P', N'PC'))
                        BEGIN
                            EXEC dbo.sp_executesql @statement = N'
                            CREATE PROCEDURE [dbo].[usp_GetAnnouncements]
                            AS
                            BEGIN
                                SELECT ID, Title, Description, Date, ShortDescription, PageTitle, IsActive, AnnouncementGuid
                                FROM Announcement
                                ORDER BY Date DESC
                            END'
                        END";
                    using (var cmd = new SqlCommand(createSpGetAnnouncements, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 15. Create usp_GetAnnouncementById
                    string createSpGetAnnouncementById = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetAnnouncementById]') AND type in (N'P', N'PC'))
                        BEGIN
                            EXEC dbo.sp_executesql @statement = N'
                            CREATE PROCEDURE [dbo].[usp_GetAnnouncementById]
                                @ID INT
                            AS
                            BEGIN
                                SELECT ID, Title, Description, Date, ShortDescription, PageTitle, IsActive, AnnouncementGuid
                                FROM Announcement
                                WHERE ID = @ID
                            END'
                        END";
                    using (var cmd = new SqlCommand(createSpGetAnnouncementById, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 16. Create usp_InsertAnnouncement
                    string createSpInsertAnnouncement = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_InsertAnnouncement]') AND type in (N'P', N'PC'))
                        BEGIN
                            EXEC dbo.sp_executesql @statement = N'
                            CREATE PROCEDURE [dbo].[usp_InsertAnnouncement]
                                @Title NVARCHAR(50),
                                @Description NVARCHAR(MAX),
                                @Date DATETIME,
                                @ShortDescription NVARCHAR(500),
                                @PageTitle NVARCHAR(200),
                                @IsActive BIT,
                                @AnnouncementGuid UNIQUEIDENTIFIER
                            AS
                            BEGIN
                                INSERT INTO Announcement (Title, Description, Date, ShortDescription, PageTitle, IsActive, AnnouncementGuid)
                                VALUES (@Title, @Description, @Date, @ShortDescription, @PageTitle, @IsActive, @AnnouncementGuid);
                                SELECT SCOPE_IDENTITY();
                            END'
                        END";
                    using (var cmd = new SqlCommand(createSpInsertAnnouncement, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 17. Create usp_UpdateAnnouncement
                    string createSpUpdateAnnouncement = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_UpdateAnnouncement]') AND type in (N'P', N'PC'))
                        BEGIN
                            EXEC dbo.sp_executesql @statement = N'
                            CREATE PROCEDURE [dbo].[usp_UpdateAnnouncement]
                                @ID INT,
                                @Title NVARCHAR(50),
                                @Description NVARCHAR(MAX),
                                @Date DATETIME,
                                @ShortDescription NVARCHAR(500),
                                @PageTitle NVARCHAR(200),
                                @IsActive BIT,
                                @AnnouncementGuid UNIQUEIDENTIFIER
                            AS
                            BEGIN
                                UPDATE Announcement SET
                                    Title = @Title,
                                    Description = @Description,
                                    Date = @Date,
                                    ShortDescription = @ShortDescription,
                                    PageTitle = @PageTitle,
                                    IsActive = @IsActive,
                                    AnnouncementGuid = @AnnouncementGuid
                                WHERE ID = @ID
                            END'
                        END";
                    using (var cmd = new SqlCommand(createSpUpdateAnnouncement, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 18. Create usp_DeleteAnnouncement
                    string createSpDeleteAnnouncement = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_DeleteAnnouncement]') AND type in (N'P', N'PC'))
                        BEGIN
                            EXEC dbo.sp_executesql @statement = N'
                            CREATE PROCEDURE [dbo].[usp_DeleteAnnouncement]
                                @ID INT
                            AS
                            BEGIN
                                DELETE FROM Announcement WHERE ID = @ID;
                            END'
                        END";
                    using (var cmd = new SqlCommand(createSpDeleteAnnouncement, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 19. Create usp_GetActiveAnnouncements
                    string createSpGetActiveAnnouncements = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetActiveAnnouncements]') AND type in (N'P', N'PC'))
                        BEGIN
                            EXEC dbo.sp_executesql @statement = N'
                            CREATE PROCEDURE [dbo].[usp_GetActiveAnnouncements]
                            AS
                            BEGIN
                                SELECT ID, Title, Description, Date, ShortDescription, PageTitle, IsActive, AnnouncementGuid
                                FROM Announcement
                                WHERE IsActive = 1
                                ORDER BY Date DESC
                            END'
                        END";
                    using (var cmd = new SqlCommand(createSpGetActiveAnnouncements, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 20. Create usp_ToggleAnnouncementActive
                    string createSpToggleAnnouncementActive = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_ToggleAnnouncementActive]') AND type in (N'P', N'PC'))
                        BEGIN
                            EXEC dbo.sp_executesql @statement = N'
                            CREATE PROCEDURE [dbo].[usp_ToggleAnnouncementActive]
                                @ID INT,
                                @IsActive BIT
                            AS
                            BEGIN
                                UPDATE Announcement SET IsActive = @IsActive WHERE ID = @ID;
                            END'
                        END";
                    using (var cmd = new SqlCommand(createSpToggleAnnouncementActive, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // ===== Users Table & Stored Procedures =====

                    // Existing installations may have been created before the
                    // account-status column was introduced. Apply this migration
                    // before referencing IsActive in the table-creation script.
                    string addUsersIsActiveColumn = @"
                        IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
                           AND COL_LENGTH(N'[dbo].[Users]', N'IsActive') IS NULL
                        BEGIN
                            ALTER TABLE [dbo].[Users]
                            ADD [IsActive] [bit] NOT NULL
                                CONSTRAINT [DF_Users_IsActive] DEFAULT (1) WITH VALUES
                        END";
                    using (var cmd = new SqlCommand(addUsersIsActiveColumn, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 21. Create Users table if not exists
                    string createUsersTable = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND type in (N'U'))
                        BEGIN
                            CREATE TABLE [dbo].[Users](
                                [ID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                [Username] [nvarchar](50) NOT NULL,
                                [Password] [nvarchar](255) NOT NULL,
                                [IsActive] [bit] NOT NULL DEFAULT (1)
                            )
                            -- Insert a default admin user
                            INSERT INTO [dbo].[Users] (Username, Password, IsActive) VALUES ('admin', 'admin', 1)
                        END";
                    using (var cmd = new SqlCommand(createUsersTable, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 22. Create usp_ValidateUser
                    string createSpValidateUser = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_ValidateUser]') AND type in (N'P', N'PC'))
                        BEGIN
                            EXEC dbo.sp_executesql @statement = N'
                            CREATE PROCEDURE [dbo].[usp_ValidateUser]
                                @Username NVARCHAR(50),
                                @Password NVARCHAR(255)
                            AS
                            BEGIN
                                SELECT COUNT(1)
                                FROM Users
                                WHERE Username = @Username AND Password = @Password AND IsActive = 1
                            END'
                        END";
                    using (var cmd = new SqlCommand(createSpValidateUser, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // ===== QualityAnnouncement Table & Stored Procedures =====
                    
                    // 23. Create QualityAnnouncement table if not exists
                    string createQualityAnnouncementTable = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[QualityAnnouncement]') AND type in (N'U'))
                        BEGIN
                            CREATE TABLE [dbo].[QualityAnnouncement](
                                [ID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                [Title] [nvarchar](50) NOT NULL,
                                [Description] [nvarchar](max) NOT NULL,
                                [Date] [datetime] NOT NULL,
                                [ShortDescription] [nvarchar](500) NOT NULL,
                                [PageTitle] [nvarchar](200) NOT NULL,
                                [IsActive] [bit] NOT NULL,
                                [AnnouncementGuid] [uniqueidentifier] NULL
                            )
                        END";
                    using (var cmd = new SqlCommand(createQualityAnnouncementTable, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 24. Create usp_GetQualityAnnouncements
                    string createSpGetQualityAnnouncements = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetQualityAnnouncements]') AND type in (N'P', N'PC'))
                        BEGIN
                            EXEC dbo.sp_executesql @statement = N'
                            CREATE PROCEDURE [dbo].[usp_GetQualityAnnouncements]
                            AS
                            BEGIN
                                SELECT ID, Title, Description, Date, ShortDescription, PageTitle, IsActive, AnnouncementGuid
                                FROM QualityAnnouncement
                                ORDER BY Date DESC
                            END'
                        END";
                    using (var cmd = new SqlCommand(createSpGetQualityAnnouncements, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 25. Create usp_GetQualityAnnouncementById
                    string createSpGetQualityAnnouncementById = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetQualityAnnouncementById]') AND type in (N'P', N'PC'))
                        BEGIN
                            EXEC dbo.sp_executesql @statement = N'
                            CREATE PROCEDURE [dbo].[usp_GetQualityAnnouncementById]
                                @ID INT
                            AS
                            BEGIN
                                SELECT ID, Title, Description, Date, ShortDescription, PageTitle, IsActive, AnnouncementGuid
                                FROM QualityAnnouncement
                                WHERE ID = @ID
                            END'
                        END";
                    using (var cmd = new SqlCommand(createSpGetQualityAnnouncementById, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 26. Create usp_InsertQualityAnnouncement
                    string createSpInsertQualityAnnouncement = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_InsertQualityAnnouncement]') AND type in (N'P', N'PC'))
                        BEGIN
                            EXEC dbo.sp_executesql @statement = N'
                            CREATE PROCEDURE [dbo].[usp_InsertQualityAnnouncement]
                                @Title NVARCHAR(50),
                                @Description NVARCHAR(MAX),
                                @Date DATETIME,
                                @ShortDescription NVARCHAR(500),
                                @PageTitle NVARCHAR(200),
                                @IsActive BIT,
                                @AnnouncementGuid UNIQUEIDENTIFIER
                            AS
                            BEGIN
                                INSERT INTO QualityAnnouncement (Title, Description, Date, ShortDescription, PageTitle, IsActive, AnnouncementGuid)
                                VALUES (@Title, @Description, @Date, @ShortDescription, @PageTitle, @IsActive, @AnnouncementGuid);
                                SELECT SCOPE_IDENTITY();
                            END'
                        END";
                    using (var cmd = new SqlCommand(createSpInsertQualityAnnouncement, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 27. Create usp_UpdateQualityAnnouncement
                    string createSpUpdateQualityAnnouncement = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_UpdateQualityAnnouncement]') AND type in (N'P', N'PC'))
                        BEGIN
                            EXEC dbo.sp_executesql @statement = N'
                            CREATE PROCEDURE [dbo].[usp_UpdateQualityAnnouncement]
                                @ID INT,
                                @Title NVARCHAR(50),
                                @Description NVARCHAR(MAX),
                                @Date DATETIME,
                                @ShortDescription NVARCHAR(500),
                                @PageTitle NVARCHAR(200),
                                @IsActive BIT,
                                @AnnouncementGuid UNIQUEIDENTIFIER
                            AS
                            BEGIN
                                UPDATE QualityAnnouncement SET
                                    Title = @Title,
                                    Description = @Description,
                                    Date = @Date,
                                    ShortDescription = @ShortDescription,
                                    PageTitle = @PageTitle,
                                    IsActive = @IsActive,
                                    AnnouncementGuid = @AnnouncementGuid
                                WHERE ID = @ID
                            END'
                        END";
                    using (var cmd = new SqlCommand(createSpUpdateQualityAnnouncement, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 28. Create usp_DeleteQualityAnnouncement
                    string createSpDeleteQualityAnnouncement = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_DeleteQualityAnnouncement]') AND type in (N'P', N'PC'))
                        BEGIN
                            EXEC dbo.sp_executesql @statement = N'
                            CREATE PROCEDURE [dbo].[usp_DeleteQualityAnnouncement]
                                @ID INT
                            AS
                            BEGIN
                                DELETE FROM QualityAnnouncement WHERE ID = @ID;
                            END'
                        END";
                    using (var cmd = new SqlCommand(createSpDeleteQualityAnnouncement, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 29. Create usp_GetActiveQualityAnnouncements
                    string createSpGetActiveQualityAnnouncements = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetActiveQualityAnnouncements]') AND type in (N'P', N'PC'))
                        BEGIN
                            EXEC dbo.sp_executesql @statement = N'
                            CREATE PROCEDURE [dbo].[usp_GetActiveQualityAnnouncements]
                            AS
                            BEGIN
                                SELECT ID, Title, Description, Date, ShortDescription, PageTitle, IsActive, AnnouncementGuid
                                FROM QualityAnnouncement
                                WHERE IsActive = 1
                                ORDER BY Date DESC
                            END'
                        END";
                    using (var cmd = new SqlCommand(createSpGetActiveQualityAnnouncements, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 30. Create usp_ToggleQualityAnnouncementActive
                    string createSpToggleQualityAnnouncementActive = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_ToggleQualityAnnouncementActive]') AND type in (N'P', N'PC'))
                        BEGIN
                            EXEC dbo.sp_executesql @statement = N'
                            CREATE PROCEDURE [dbo].[usp_ToggleQualityAnnouncementActive]
                                @ID INT,
                                @IsActive BIT
                            AS
                            BEGIN
                                UPDATE QualityAnnouncement SET IsActive = @IsActive WHERE ID = @ID;
                            END'
                        END";
                    using (var cmd = new SqlCommand(createSpToggleQualityAnnouncementActive, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // ===== HrAnnouncement Table & Stored Procedures =====
                    
                    // 31. Create HrAnnouncement table if not exists
                    string createHrAnnouncementTable = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[HrAnnouncement]') AND type in (N'U'))
                        BEGIN
                            CREATE TABLE [dbo].[HrAnnouncement](
                                [ID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                [Title] [nvarchar](50) NOT NULL,
                                [Description] [nvarchar](max) NOT NULL,
                                [Date] [datetime] NOT NULL,
                                [ShortDescription] [nvarchar](500) NOT NULL,
                                [PageTitle] [nvarchar](200) NOT NULL,
                                [IsActive] [bit] NOT NULL,
                                [AnnouncementGuid] [uniqueidentifier] NULL
                            )
                        END";
                    using (var cmd = new SqlCommand(createHrAnnouncementTable, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 32. Create usp_GetHrAnnouncements
                    string createSpGetHrAnnouncements = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetHrAnnouncements]') AND type in (N'P', N'PC'))
                        BEGIN
                            EXEC dbo.sp_executesql @statement = N'
                            CREATE PROCEDURE [dbo].[usp_GetHrAnnouncements]
                            AS
                            BEGIN
                                SELECT ID, Title, Description, Date, ShortDescription, PageTitle, IsActive, AnnouncementGuid
                                FROM HrAnnouncement
                                ORDER BY Date DESC
                            END'
                        END";
                    using (var cmd = new SqlCommand(createSpGetHrAnnouncements, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 33. Create usp_GetHrAnnouncementById
                    string createSpGetHrAnnouncementById = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetHrAnnouncementById]') AND type in (N'P', N'PC'))
                        BEGIN
                            EXEC dbo.sp_executesql @statement = N'
                            CREATE PROCEDURE [dbo].[usp_GetHrAnnouncementById]
                                @ID INT
                            AS
                            BEGIN
                                SELECT ID, Title, Description, Date, ShortDescription, PageTitle, IsActive, AnnouncementGuid
                                FROM HrAnnouncement
                                WHERE ID = @ID
                            END'
                        END";
                    using (var cmd = new SqlCommand(createSpGetHrAnnouncementById, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 34. Create usp_InsertHrAnnouncement
                    string createSpInsertHrAnnouncement = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_InsertHrAnnouncement]') AND type in (N'P', N'PC'))
                        BEGIN
                            EXEC dbo.sp_executesql @statement = N'
                            CREATE PROCEDURE [dbo].[usp_InsertHrAnnouncement]
                                @Title NVARCHAR(50),
                                @Description NVARCHAR(MAX),
                                @Date DATETIME,
                                @ShortDescription NVARCHAR(500),
                                @PageTitle NVARCHAR(200),
                                @IsActive BIT,
                                @AnnouncementGuid UNIQUEIDENTIFIER
                            AS
                            BEGIN
                                INSERT INTO HrAnnouncement (Title, Description, Date, ShortDescription, PageTitle, IsActive, AnnouncementGuid)
                                VALUES (@Title, @Description, @Date, @ShortDescription, @PageTitle, @IsActive, @AnnouncementGuid);
                                SELECT SCOPE_IDENTITY();
                            END'
                        END";
                    using (var cmd = new SqlCommand(createSpInsertHrAnnouncement, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 35. Create usp_UpdateHrAnnouncement
                    string createSpUpdateHrAnnouncement = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_UpdateHrAnnouncement]') AND type in (N'P', N'PC'))
                        BEGIN
                            EXEC dbo.sp_executesql @statement = N'
                            CREATE PROCEDURE [dbo].[usp_UpdateHrAnnouncement]
                                @ID INT,
                                @Title NVARCHAR(50),
                                @Description NVARCHAR(MAX),
                                @Date DATETIME,
                                @ShortDescription NVARCHAR(500),
                                @PageTitle NVARCHAR(200),
                                @IsActive BIT,
                                @AnnouncementGuid UNIQUEIDENTIFIER
                            AS
                            BEGIN
                                UPDATE HrAnnouncement SET
                                    Title = @Title,
                                    Description = @Description,
                                    Date = @Date,
                                    ShortDescription = @ShortDescription,
                                    PageTitle = @PageTitle,
                                    IsActive = @IsActive,
                                    AnnouncementGuid = @AnnouncementGuid
                                WHERE ID = @ID
                            END'
                        END";
                    using (var cmd = new SqlCommand(createSpUpdateHrAnnouncement, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 36. Create usp_DeleteHrAnnouncement
                    string createSpDeleteHrAnnouncement = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_DeleteHrAnnouncement]') AND type in (N'P', N'PC'))
                        BEGIN
                            EXEC dbo.sp_executesql @statement = N'
                            CREATE PROCEDURE [dbo].[usp_DeleteHrAnnouncement]
                                @ID INT
                            AS
                            BEGIN
                                DELETE FROM HrAnnouncement WHERE ID = @ID;
                            END'
                        END";
                    using (var cmd = new SqlCommand(createSpDeleteHrAnnouncement, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 37. Create usp_GetActiveHrAnnouncements
                    string createSpGetActiveHrAnnouncements = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetActiveHrAnnouncements]') AND type in (N'P', N'PC'))
                        BEGIN
                            EXEC dbo.sp_executesql @statement = N'
                            CREATE PROCEDURE [dbo].[usp_GetActiveHrAnnouncements]
                            AS
                            BEGIN
                                SELECT ID, Title, Description, Date, ShortDescription, PageTitle, IsActive, AnnouncementGuid
                                FROM HrAnnouncement
                                WHERE IsActive = 1
                                ORDER BY Date DESC
                            END'
                        END";
                    using (var cmd = new SqlCommand(createSpGetActiveHrAnnouncements, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 38. Create usp_ToggleHrAnnouncementActive
                    string createSpToggleHrAnnouncementActive = @"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_ToggleHrAnnouncementActive]') AND type in (N'P', N'PC'))
                        BEGIN
                            EXEC dbo.sp_executesql @statement = N'
                            CREATE PROCEDURE [dbo].[usp_ToggleHrAnnouncementActive]
                                @ID INT,
                                @IsActive BIT
                            AS
                            BEGIN
                                UPDATE HrAnnouncement SET IsActive = @IsActive WHERE ID = @ID;
                            END'
                        END";
                    using (var cmd = new SqlCommand(createSpToggleHrAnnouncementActive, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                System.Diagnostics.Debug.WriteLine("DatabaseInitializer: DB Initialized successfully.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("DatabaseInitializer Error: " + ex.Message);
            }
        }
    }
}
