USE [master]
GO
/****** Object:  Database [SenseLibDB]    Script Date: 01-Jun-25 11:24:01 PM ******/
CREATE DATABASE [SenseLibDB]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'SenseLibDB', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\DATA\SenseLibDB.mdf' , SIZE = 73728KB , MAXSIZE = UNLIMITED, FILEGROWTH = 65536KB )
 LOG ON 
( NAME = N'SenseLibDB_log', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\DATA\SenseLibDB_log.ldf' , SIZE = 8192KB , MAXSIZE = 2048GB , FILEGROWTH = 65536KB )
 WITH CATALOG_COLLATION = DATABASE_DEFAULT, LEDGER = OFF
GO
ALTER DATABASE [SenseLibDB] SET COMPATIBILITY_LEVEL = 160
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [SenseLibDB].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [SenseLibDB] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [SenseLibDB] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [SenseLibDB] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [SenseLibDB] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [SenseLibDB] SET ARITHABORT OFF 
GO
ALTER DATABASE [SenseLibDB] SET AUTO_CLOSE ON 
GO
ALTER DATABASE [SenseLibDB] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [SenseLibDB] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [SenseLibDB] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [SenseLibDB] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [SenseLibDB] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [SenseLibDB] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [SenseLibDB] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [SenseLibDB] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [SenseLibDB] SET  ENABLE_BROKER 
GO
ALTER DATABASE [SenseLibDB] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [SenseLibDB] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [SenseLibDB] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [SenseLibDB] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [SenseLibDB] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [SenseLibDB] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [SenseLibDB] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [SenseLibDB] SET RECOVERY SIMPLE 
GO
ALTER DATABASE [SenseLibDB] SET  MULTI_USER 
GO
ALTER DATABASE [SenseLibDB] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [SenseLibDB] SET DB_CHAINING OFF 
GO
ALTER DATABASE [SenseLibDB] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [SenseLibDB] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [SenseLibDB] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [SenseLibDB] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO
ALTER DATABASE [SenseLibDB] SET QUERY_STORE = ON
GO
ALTER DATABASE [SenseLibDB] SET QUERY_STORE (OPERATION_MODE = READ_WRITE, CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30), DATA_FLUSH_INTERVAL_SECONDS = 900, INTERVAL_LENGTH_MINUTES = 60, MAX_STORAGE_SIZE_MB = 1000, QUERY_CAPTURE_MODE = AUTO, SIZE_BASED_CLEANUP_MODE = AUTO, MAX_PLANS_PER_QUERY = 200, WAIT_STATS_CAPTURE_MODE = ON)
GO
USE [SenseLibDB]
GO
/****** Object:  Table [dbo].[__EFMigrationsHistory]    Script Date: 01-Jun-25 11:24:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[__EFMigrationsHistory](
	[MigrationId] [nvarchar](150) NOT NULL,
	[ProductVersion] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY CLUSTERED 
(
	[MigrationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AdminMenu]    Script Date: 01-Jun-25 11:24:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AdminMenu](
	[MenuID] [int] IDENTITY(1,1) NOT NULL,
	[MenuName] [nvarchar](max) NOT NULL,
	[ItemLevel] [int] NOT NULL,
	[IsActive] [bit] NOT NULL,
	[ParentLevel] [int] NOT NULL,
	[ItemOrder] [int] NOT NULL,
	[ItemTarget] [nvarchar](max) NULL,
	[AreaName] [nvarchar](max) NULL,
	[ControllerName] [nvarchar](max) NULL,
	[ActionName] [nvarchar](max) NULL,
	[Icon] [nvarchar](max) NULL,
	[IdName] [nvarchar](max) NULL,
 CONSTRAINT [PK_AdminMenu] PRIMARY KEY CLUSTERED 
(
	[MenuID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Authors]    Script Date: 01-Jun-25 11:24:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Authors](
	[AuthorID] [int] IDENTITY(1,1) NOT NULL,
	[AuthorName] [nvarchar](100) NOT NULL,
	[Bio] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_Authors] PRIMARY KEY CLUSTERED 
(
	[AuthorID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Categories]    Script Date: 01-Jun-25 11:24:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Categories](
	[CategoryID] [int] IDENTITY(1,1) NOT NULL,
	[CategoryName] [nvarchar](100) NOT NULL,
	[Description] [nvarchar](max) NOT NULL,
	[Status] [nvarchar](10) NOT NULL,
 CONSTRAINT [PK_Categories] PRIMARY KEY CLUSTERED 
(
	[CategoryID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CommentLikes]    Script Date: 01-Jun-25 11:24:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CommentLikes](
	[CommentLikeID] [int] IDENTITY(1,1) NOT NULL,
	[CommentID] [int] NOT NULL,
	[UserID] [int] NOT NULL,
	[LikeDate] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[CommentLikeID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Comments]    Script Date: 01-Jun-25 11:24:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Comments](
	[CommentID] [int] IDENTITY(1,1) NOT NULL,
	[DocumentID] [int] NOT NULL,
	[UserID] [int] NOT NULL,
	[CommentText] [nvarchar](max) NOT NULL,
	[CommentDate] [datetime2](7) NOT NULL,
	[LikeCount] [int] NOT NULL,
	[ParentCommentID] [int] NULL,
 CONSTRAINT [PK_Comments] PRIMARY KEY CLUSTERED 
(
	[CommentID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ContactMessages]    Script Date: 01-Jun-25 11:24:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ContactMessages](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](max) NOT NULL,
	[Email] [nvarchar](max) NOT NULL,
	[Subject] [nvarchar](max) NOT NULL,
	[Message] [nvarchar](max) NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[IsRead] [bit] NOT NULL,
 CONSTRAINT [PK_ContactMessages] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Documents]    Script Date: 01-Jun-25 11:24:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Documents](
	[DocumentID] [int] IDENTITY(1,1) NOT NULL,
	[Title] [nvarchar](255) NOT NULL,
	[Description] [nvarchar](max) NOT NULL,
	[CategoryID] [int] NULL,
	[PublisherID] [int] NULL,
	[AuthorID] [int] NULL,
	[UploadDate] [datetime2](7) NOT NULL,
	[FilePath] [nvarchar](255) NOT NULL,
	[Status] [nvarchar](20) NOT NULL,
	[ImagePath] [nvarchar](255) NULL,
	[IsPaid] [bit] NOT NULL,
	[Price] [decimal](18, 2) NULL,
	[UserID] [int] NULL,
 CONSTRAINT [PK_Documents] PRIMARY KEY CLUSTERED 
(
	[DocumentID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DocumentSimilarityCache]    Script Date: 01-Jun-25 11:24:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DocumentSimilarityCache](
	[CacheID] [int] IDENTITY(1,1) NOT NULL,
	[DocumentID] [int] NOT NULL,
	[SimilarDocumentID] [int] NOT NULL,
	[SimilarityScore] [float] NOT NULL,
	[CreatedDate] [datetime] NOT NULL,
	[ExpiryDate] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[CacheID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DocumentStatistics]    Script Date: 01-Jun-25 11:24:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DocumentStatistics](
	[StatisticsID] [int] IDENTITY(1,1) NOT NULL,
	[DocumentID] [int] NOT NULL,
	[ViewCount] [int] NOT NULL,
	[LastUpdated] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_DocumentStatistics] PRIMARY KEY CLUSTERED 
(
	[StatisticsID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Downloads]    Script Date: 01-Jun-25 11:24:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Downloads](
	[DownloadID] [int] IDENTITY(1,1) NOT NULL,
	[UserID] [int] NOT NULL,
	[DocumentID] [int] NOT NULL,
	[DownloadDate] [datetime2](7) NOT NULL,
	[DownloadType] [nvarchar](20) NOT NULL,
 CONSTRAINT [PK_Downloads] PRIMARY KEY CLUSTERED 
(
	[DownloadID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Favorites]    Script Date: 01-Jun-25 11:24:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Favorites](
	[FavoriteID] [int] IDENTITY(1,1) NOT NULL,
	[UserID] [int] NOT NULL,
	[DocumentID] [int] NOT NULL,
 CONSTRAINT [PK_Favorites] PRIMARY KEY CLUSTERED 
(
	[FavoriteID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Menu]    Script Date: 01-Jun-25 11:24:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Menu](
	[MenuID] [int] IDENTITY(1,1) NOT NULL,
	[MenuName] [nvarchar](255) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[ControllerName] [nvarchar](255) NOT NULL,
	[ActionName] [nvarchar](255) NOT NULL,
	[Levels] [int] NOT NULL,
	[ParentID] [int] NOT NULL,
	[Link] [nvarchar](255) NOT NULL,
	[MenuOrder] [int] NOT NULL,
	[Position] [int] NOT NULL,
 CONSTRAINT [PK_Menu] PRIMARY KEY CLUSTERED 
(
	[MenuID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PasswordResetTokens]    Script Date: 01-Jun-25 11:24:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PasswordResetTokens](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[Token] [nvarchar](100) NOT NULL,
	[ExpiryDate] [datetime] NOT NULL,
	[IsUsed] [bit] NOT NULL,
 CONSTRAINT [PK_PasswordResetTokens] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Publishers]    Script Date: 01-Jun-25 11:24:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Publishers](
	[PublisherID] [int] IDENTITY(1,1) NOT NULL,
	[PublisherName] [nvarchar](100) NOT NULL,
	[Address] [nvarchar](255) NOT NULL,
	[Phone] [nvarchar](20) NOT NULL,
 CONSTRAINT [PK_Publishers] PRIMARY KEY CLUSTERED 
(
	[PublisherID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Purchases]    Script Date: 01-Jun-25 11:24:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Purchases](
	[PurchaseID] [int] IDENTITY(1,1) NOT NULL,
	[UserID] [int] NOT NULL,
	[DocumentID] [int] NOT NULL,
	[PurchaseDate] [datetime] NOT NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[TransactionCode] [nvarchar](100) NULL,
	[Status] [nvarchar](20) NULL,
PRIMARY KEY CLUSTERED 
(
	[PurchaseID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Ratings]    Script Date: 01-Jun-25 11:24:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Ratings](
	[RatingID] [int] IDENTITY(1,1) NOT NULL,
	[DocumentID] [int] NOT NULL,
	[UserID] [int] NOT NULL,
	[RatingValue] [int] NOT NULL,
	[RatingDate] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Ratings] PRIMARY KEY CLUSTERED 
(
	[RatingID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Slideshow]    Script Date: 01-Jun-25 11:24:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Slideshow](
	[SlideID] [int] IDENTITY(1,1) NOT NULL,
	[Title] [nvarchar](255) NOT NULL,
	[Description] [nvarchar](500) NULL,
	[ImagePath] [nvarchar](255) NOT NULL,
	[Link] [nvarchar](255) NULL,
	[DisplayOrder] [int] NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Slideshow] PRIMARY KEY CLUSTERED 
(
	[SlideID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SystemConfigs]    Script Date: 01-Jun-25 11:24:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SystemConfigs](
	[ConfigID] [int] IDENTITY(1,1) NOT NULL,
	[ConfigKey] [nvarchar](100) NOT NULL,
	[ConfigValue] [nvarchar](1000) NOT NULL,
	[Description] [nvarchar](500) NULL,
PRIMARY KEY CLUSTERED 
(
	[ConfigID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Transactions]    Script Date: 01-Jun-25 11:24:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Transactions](
	[TransactionID] [int] IDENTITY(1,1) NOT NULL,
	[UserID] [int] NOT NULL,
	[TransactionType] [nvarchar](20) NOT NULL,
	[TransactionDate] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Transactions] PRIMARY KEY CLUSTERED 
(
	[TransactionID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UserActivities]    Script Date: 01-Jun-25 11:24:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserActivities](
	[ActivityID] [int] IDENTITY(1,1) NOT NULL,
	[UserID] [int] NOT NULL,
	[ActivityDate] [datetime] NOT NULL,
	[ActivityType] [nvarchar](50) NOT NULL,
	[DocumentID] [int] NULL,
	[CommentID] [int] NULL,
	[Description] [nvarchar](500) NULL,
	[AdditionalData] [nvarchar](1000) NULL,
PRIMARY KEY CLUSTERED 
(
	[ActivityID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Users]    Script Date: 01-Jun-25 11:24:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Users](
	[UserID] [int] IDENTITY(1,1) NOT NULL,
	[Username] [nvarchar](50) NOT NULL,
	[Password] [nvarchar](255) NOT NULL,
	[Email] [nvarchar](100) NOT NULL,
	[FullName] [nvarchar](100) NOT NULL,
	[Role] [nvarchar](20) NOT NULL,
	[Status] [nvarchar](20) NOT NULL,
	[ProfileImage] [nvarchar](max) NULL,
	[LoginAttempts] [int] NOT NULL,
	[LockoutEnd] [datetime] NULL,
 CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED 
(
	[UserID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Wallets]    Script Date: 01-Jun-25 11:24:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Wallets](
	[WalletID] [int] IDENTITY(1,1) NOT NULL,
	[UserID] [int] NOT NULL,
	[Balance] [decimal](18, 2) NOT NULL,
	[CreatedDate] [datetime] NOT NULL,
	[LastUpdatedDate] [datetime] NOT NULL,
 CONSTRAINT [PK_Wallets] PRIMARY KEY CLUSTERED 
(
	[WalletID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[WalletTransactions]    Script Date: 01-Jun-25 11:24:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[WalletTransactions](
	[TransactionID] [int] IDENTITY(1,1) NOT NULL,
	[WalletID] [int] NOT NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[TransactionDate] [datetime] NOT NULL,
	[Type] [nvarchar](50) NOT NULL,
	[Description] [nvarchar](255) NULL,
	[DocumentID] [int] NULL,
	[PurchaseID] [int] NULL,
 CONSTRAINT [PK_WalletTransactions] PRIMARY KEY CLUSTERED 
(
	[TransactionID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
INSERT [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20250511175657_InitialCreate', N'9.0.4')
INSERT [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20250512112221_AddDocumentStatistics', N'9.0.4')
INSERT [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20250512145240_FavoriteFixMigration', N'9.0.4')
INSERT [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20250512153826_AddDocumentImagePath', N'8.0.4')
INSERT [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20250513142743_AddDocumentIsPaidAndUploadUser', N'8.0.4')
GO
SET IDENTITY_INSERT [dbo].[AdminMenu] ON 

INSERT [dbo].[AdminMenu] ([MenuID], [MenuName], [ItemLevel], [IsActive], [ParentLevel], [ItemOrder], [ItemTarget], [AreaName], [ControllerName], [ActionName], [Icon], [IdName]) VALUES (3, N'Quản lý menu', 1, 1, 0, 1, N'menu-nav', N'Admin', N'Menu', N'Index', N'bi bi-menu-button-fill', N'menu-nav')
INSERT [dbo].[AdminMenu] ([MenuID], [MenuName], [ItemLevel], [IsActive], [ParentLevel], [ItemOrder], [ItemTarget], [AreaName], [ControllerName], [ActionName], [Icon], [IdName]) VALUES (4, N'Quản lý slideshow', 1, 1, 0, 1, N'slideshow-nav', N'Admin', N'Slideshow', N'Index', N'bi bi-images', N'slideshow-nav')
INSERT [dbo].[AdminMenu] ([MenuID], [MenuName], [ItemLevel], [IsActive], [ParentLevel], [ItemOrder], [ItemTarget], [AreaName], [ControllerName], [ActionName], [Icon], [IdName]) VALUES (5, N'Tài liệu', 1, 1, 0, 1, NULL, N'Admin', N'Document', N'Index', N'bi bi-journal-text', NULL)
INSERT [dbo].[AdminMenu] ([MenuID], [MenuName], [ItemLevel], [IsActive], [ParentLevel], [ItemOrder], [ItemTarget], [AreaName], [ControllerName], [ActionName], [Icon], [IdName]) VALUES (6, N'Quản lý danh mục', 1, 1, 0, 1, NULL, N'Admin', N'Category', N'Index', N'bi bi-folder', NULL)
INSERT [dbo].[AdminMenu] ([MenuID], [MenuName], [ItemLevel], [IsActive], [ParentLevel], [ItemOrder], [ItemTarget], [AreaName], [ControllerName], [ActionName], [Icon], [IdName]) VALUES (7, N'Quản lý tác giả', 1, 1, 0, 1, NULL, N'Admin', N'Author', N'Index', N'bi bi-people', NULL)
INSERT [dbo].[AdminMenu] ([MenuID], [MenuName], [ItemLevel], [IsActive], [ParentLevel], [ItemOrder], [ItemTarget], [AreaName], [ControllerName], [ActionName], [Icon], [IdName]) VALUES (8, N'Quản lý nhà xuất bản', 1, 1, 0, 1, NULL, N'Admin', N'Publisher', N'Index', N'bi bi-buildings', NULL)
INSERT [dbo].[AdminMenu] ([MenuID], [MenuName], [ItemLevel], [IsActive], [ParentLevel], [ItemOrder], [ItemTarget], [AreaName], [ControllerName], [ActionName], [Icon], [IdName]) VALUES (11, N'Quản lý File', 1, 0, 0, 1, NULL, N'Admin', N'FileManager', N'Index', N'bi bi-folder', NULL)
INSERT [dbo].[AdminMenu] ([MenuID], [MenuName], [ItemLevel], [IsActive], [ParentLevel], [ItemOrder], [ItemTarget], [AreaName], [ControllerName], [ActionName], [Icon], [IdName]) VALUES (12, N'Phản hồi', 1, 1, 0, 1, NULL, N'Admin', N'Contact', N'Index', N'bi bi-chat-left-dots', NULL)
INSERT [dbo].[AdminMenu] ([MenuID], [MenuName], [ItemLevel], [IsActive], [ParentLevel], [ItemOrder], [ItemTarget], [AreaName], [ControllerName], [ActionName], [Icon], [IdName]) VALUES (13, N'Quản lý tài khoản', 1, 1, 0, 1, NULL, N'Admin', N'User', N'Index', N'bi bi-people', NULL)
INSERT [dbo].[AdminMenu] ([MenuID], [MenuName], [ItemLevel], [IsActive], [ParentLevel], [ItemOrder], [ItemTarget], [AreaName], [ControllerName], [ActionName], [Icon], [IdName]) VALUES (14, N'Thống kê', 1, 1, 0, 1, NULL, N'Admin', N'Statistics', N'Index', N'bi bi-graph-up', N'statistics-nav')
INSERT [dbo].[AdminMenu] ([MenuID], [MenuName], [ItemLevel], [IsActive], [ParentLevel], [ItemOrder], [ItemTarget], [AreaName], [ControllerName], [ActionName], [Icon], [IdName]) VALUES (15, N'Bình luận', 2, 1, 14, 1, NULL, N'Admin', N'Statistics', N'Comments', N'bi bi-chat-dots', NULL)
INSERT [dbo].[AdminMenu] ([MenuID], [MenuName], [ItemLevel], [IsActive], [ParentLevel], [ItemOrder], [ItemTarget], [AreaName], [ControllerName], [ActionName], [Icon], [IdName]) VALUES (16, N'Đánh giá', 2, 1, 14, 2, NULL, N'Admin', N'Statistics', N'Ratings', N'bi bi-star', NULL)
INSERT [dbo].[AdminMenu] ([MenuID], [MenuName], [ItemLevel], [IsActive], [ParentLevel], [ItemOrder], [ItemTarget], [AreaName], [ControllerName], [ActionName], [Icon], [IdName]) VALUES (17, N'Yêu thích', 2, 1, 14, 3, NULL, N'Admin', N'Statistics', N'Favorites', N'bi bi-heart', NULL)
INSERT [dbo].[AdminMenu] ([MenuID], [MenuName], [ItemLevel], [IsActive], [ParentLevel], [ItemOrder], [ItemTarget], [AreaName], [ControllerName], [ActionName], [Icon], [IdName]) VALUES (18, N'Tải xuống', 2, 1, 14, 4, NULL, N'Admin', N'Statistics', N'Downloads', N'bi bi-download', NULL)
INSERT [dbo].[AdminMenu] ([MenuID], [MenuName], [ItemLevel], [IsActive], [ParentLevel], [ItemOrder], [ItemTarget], [AreaName], [ControllerName], [ActionName], [Icon], [IdName]) VALUES (19, N'Lịch sử thanh toán', 1, 1, 0, 1, NULL, N'Admin', N'Purchase', N'Index', N'bi bi-upc-scan', NULL)
INSERT [dbo].[AdminMenu] ([MenuID], [MenuName], [ItemLevel], [IsActive], [ParentLevel], [ItemOrder], [ItemTarget], [AreaName], [ControllerName], [ActionName], [Icon], [IdName]) VALUES (20, N'Cấu hình hệ thống', 1, 1, 0, 1, NULL, N'Admin', N'SystemConfig', N'Index', N'bi bi-gear-fill', NULL)
SET IDENTITY_INSERT [dbo].[AdminMenu] OFF
GO
SET IDENTITY_INSERT [dbo].[Authors] ON 

INSERT [dbo].[Authors] ([AuthorID], [AuthorName], [Bio]) VALUES (5, N'Vũ Hữu Tiệp', N'Vũ Hữu Tiệp, tốt nghiệp Tiến sĩ ngành Học Máy và Thị Giác Máy Tính (Machine Learning and Computer Vision) tại Đại học bang Pennsylvania (Pennsylvania State University), Hoa Kỳ.')
INSERT [dbo].[Authors] ([AuthorID], [AuthorName], [Bio]) VALUES (6, N'Olga Filipova', N'Olga Filipova là một lập trình viên có kinh nghiệm trong phát triển fontend, chính vì vậy các nội dung được viết ra trong Learning Vue.js 2 là rất sát với thực tế.')
INSERT [dbo].[Authors] ([AuthorID], [AuthorName], [Bio]) VALUES (7, N'Mỹ Hạnh', N'Chị là hội viên Hội Văn học Nghệ thuật tỉnh Lâm Đồng, bắt đầu có thơ đăng báo từ năm 1972. Chị còn có bút hiệu Đơn Phương Thạch Thảo, có 49 bài thơ được các nhạc sĩ phổ nhạc và có thơ đăng trong 19 tập thơ tuyển.')
INSERT [dbo].[Authors] ([AuthorID], [AuthorName], [Bio]) VALUES (8, N'Nhật Nguyên', N'Được xem là một trong những nhà văn hiện đại xuất sắc nhất Việt Nam hiện nay, ông được biết đến qua nhiều tác phẩm văn học về đề tài tuổi trẻ. Nhiều tác phẩm của ông được độc giả và giới chuyên môn đánh giá cao, đa số đều đã được chuyển thể thành phim.')
INSERT [dbo].[Authors] ([AuthorID], [AuthorName], [Bio]) VALUES (9, N'Nhiều tác giả', N'Nhiều tác giả cùng nhau soạn thảo')
INSERT [dbo].[Authors] ([AuthorID], [AuthorName], [Bio]) VALUES (10, N'hiếu', N'Người dùng tải lên tài liệu')
INSERT [dbo].[Authors] ([AuthorID], [AuthorName], [Bio]) VALUES (11, N'NGUYEN VAN HIEU', N'Người dùng tải lên tài liệu')
SET IDENTITY_INSERT [dbo].[Authors] OFF
GO
SET IDENTITY_INSERT [dbo].[Categories] ON 

INSERT [dbo].[Categories] ([CategoryID], [CategoryName], [Description], [Status]) VALUES (4, N'Công nghệ thông tin', N'Những cuốn sách về công nghệ, sách công nghệ thông tin hay cho người mới bắt đầu nên học. ', N'Active')
INSERT [dbo].[Categories] ([CategoryID], [CategoryName], [Description], [Status]) VALUES (5, N'Ẩm thực', N'Tủ sách Văn hóa Ẩm thực hướng tới việc khám phá và tôn vinh những giá trị ẩm thực độc đáo của các nền văn hóa trên thế giới.', N'Active')
INSERT [dbo].[Categories] ([CategoryID], [CategoryName], [Description], [Status]) VALUES (6, N'Ngoại ngữ', N'Thư viện hiện có một danh mục sách ngoại ngữ phong phú, phục vụ nhu cầu học tập và nghiên cứu của sinh viên. ', N'Active')
SET IDENTITY_INSERT [dbo].[Categories] OFF
GO
SET IDENTITY_INSERT [dbo].[CommentLikes] ON 

INSERT [dbo].[CommentLikes] ([CommentLikeID], [CommentID], [UserID], [LikeDate]) VALUES (8, 24, 9, CAST(N'2025-05-23T22:31:53.790' AS DateTime))
SET IDENTITY_INSERT [dbo].[CommentLikes] OFF
GO
SET IDENTITY_INSERT [dbo].[Comments] ON 

INSERT [dbo].[Comments] ([CommentID], [DocumentID], [UserID], [CommentText], [CommentDate], [LikeCount], [ParentCommentID]) VALUES (21, 27, 3, N'tài liệu hay quá, chúc cả nhà ngày mới vui vẻ <3', CAST(N'2025-05-18T10:14:06.1691593' AS DateTime2), 0, NULL)
INSERT [dbo].[Comments] ([CommentID], [DocumentID], [UserID], [CommentText], [CommentDate], [LikeCount], [ParentCommentID]) VALUES (22, 32, 3, N'xin chào
', CAST(N'2025-05-18T10:58:37.4249293' AS DateTime2), 0, NULL)
INSERT [dbo].[Comments] ([CommentID], [DocumentID], [UserID], [CommentText], [CommentDate], [LikeCount], [ParentCommentID]) VALUES (23, 32, 4, N'rất hay', CAST(N'2025-05-18T11:20:34.4090799' AS DateTime2), 0, NULL)
INSERT [dbo].[Comments] ([CommentID], [DocumentID], [UserID], [CommentText], [CommentDate], [LikeCount], [ParentCommentID]) VALUES (24, 34, 9, N'hi', CAST(N'2025-05-23T22:31:48.7510768' AS DateTime2), 1, NULL)
INSERT [dbo].[Comments] ([CommentID], [DocumentID], [UserID], [CommentText], [CommentDate], [LikeCount], [ParentCommentID]) VALUES (25, 36, 9, N'hi', CAST(N'2025-05-23T22:32:11.7210770' AS DateTime2), 0, NULL)
INSERT [dbo].[Comments] ([CommentID], [DocumentID], [UserID], [CommentText], [CommentDate], [LikeCount], [ParentCommentID]) VALUES (26, 32, 9, N'7tfu', CAST(N'2025-05-23T23:47:31.6895477' AS DateTime2), 0, NULL)
INSERT [dbo].[Comments] ([CommentID], [DocumentID], [UserID], [CommentText], [CommentDate], [LikeCount], [ParentCommentID]) VALUES (27, 32, 9, N'mhi', CAST(N'2025-05-23T23:47:34.1341366' AS DateTime2), 0, NULL)
SET IDENTITY_INSERT [dbo].[Comments] OFF
GO
SET IDENTITY_INSERT [dbo].[Documents] ON 

INSERT [dbo].[Documents] ([DocumentID], [Title], [Description], [CategoryID], [PublisherID], [AuthorID], [UploadDate], [FilePath], [Status], [ImagePath], [IsPaid], [Price], [UserID]) VALUES (25, N'Machine Learning Cơ Bản', N'Machine Learning là một tập con của AI. Theo định nghĩa của Wikipedia, Machine learning is the subfield of computer science that “gives computers the ability to learn without being explicitly programmed”. ', 4, 7, 5, CAST(N'2025-05-17T23:26:39.9387216' AS DateTime2), N'/uploads/documents/4581c4bb-1699-41d6-a6bb-abb545595316_📘 Machine Learning Cơ Bản.docx', N'Published', N'/uploads/images/c3bfe070-3e50-4f82-b984-32a636cb6587_nhasachmienphi-machine-learning-co-ban.jpg', 0, CAST(10000.00 AS Decimal(18, 2)), NULL)
INSERT [dbo].[Documents] ([DocumentID], [Title], [Description], [CategoryID], [PublisherID], [AuthorID], [UploadDate], [FilePath], [Status], [ImagePath], [IsPaid], [Price], [UserID]) VALUES (26, N'Learning Vue.js 2', N'Vue.js is one of the latest new frameworks to have piqued the interest of web developers due to its reactivity, reusable components, and ease of use.', 4, 5, 6, CAST(N'2025-05-18T00:33:45.1999093' AS DateTime2), N'/uploads/documents/f426511c-49b4-4431-87b0-fab8d262ef41_📘 Learning Vue.docx', N'Published', N'/uploads/images/dfa7c7d5-5afd-4dc6-ab0d-17c728eb4cef_nhasachmienphi-learning-vue-js-2.jpg', 1, CAST(10000.00 AS Decimal(18, 2)), NULL)
INSERT [dbo].[Documents] ([DocumentID], [Title], [Description], [CategoryID], [PublisherID], [AuthorID], [UploadDate], [FilePath], [Status], [ImagePath], [IsPaid], [Price], [UserID]) VALUES (27, N'Laravel 5 Cookbook Enhance Your Amazing Applications', N'Learning Laravel 5: Building Practical Applications is the easiest way to learn web development using Laravel. Throughout 5 chapters, instructor Nathan Wu will teach you how to build many real-world applications from scratch. This bestseller is also completely about you. It has been structured very carefully, teaching you all you need to know from installing your Laravel 5.1 app to deploying it to a live server.', 4, 9, 6, CAST(N'2025-05-18T09:04:33.6111888' AS DateTime2), N'/uploads/documents/0c8cd7db-95da-498d-9d56-0969cd7205f5_Laravel 5 Cookbook.docx', N'Approved', N'/uploads/images/b57636a8-7db8-4e0b-89f5-cbca4ee306a9_nhasachmienphi-laravel-5-cookbook-enhance-your-amazing-applications.jpg', 1, CAST(100000.00 AS Decimal(18, 2)), 1)
INSERT [dbo].[Documents] ([DocumentID], [Title], [Description], [CategoryID], [PublisherID], [AuthorID], [UploadDate], [FilePath], [Status], [ImagePath], [IsPaid], [Price], [UserID]) VALUES (28, N'Món Ăn Giúp Trẻ Thông Minh Học Giỏi', N'Cuốn sách “Món ăn giúp trẻ thông minh học giỏi” xin giới thiệu các loại thực phẩm thông dụng hàng ngày, các món ăn bổ dưỡng giúp trẻ nhỏ thêm linh lợi, hoạt bát, giúp sĩ tử thêm vững tin trước các kỳ thi đầy gian nan.', 5, 1, 8, CAST(N'2025-05-18T09:08:55.0635223' AS DateTime2), N'/uploads/documents/752aefb6-bde3-499f-8ece-78d132d6bcb1_📘 Món Ăn Giúp Trẻ Thông Minh Học Giỏi.docx', N'Approved', N'/uploads/images/fe5c3c98-ba7b-4b16-bda7-b5b18828e213_nhasachmienphi-mon-an-giup-tre-thong-minh-hoc-gioi.jpg', 1, CAST(300000.00 AS Decimal(18, 2)), 1)
INSERT [dbo].[Documents] ([DocumentID], [Title], [Description], [CategoryID], [PublisherID], [AuthorID], [UploadDate], [FilePath], [Status], [ImagePath], [IsPaid], [Price], [UserID]) VALUES (29, N'Những Món Ăn Chay Nổi Tiếng', N'Thực ra, các món chay không chỉ ngon miệng, cung cấp đủ chất dinh dưỡng mà còn dễ thực hiện. “Những món ăn chay nổi tiếng” là cẩm nang ẩm thực chay hoàn hảo, nó hấp dẫn ngay cả những người ăn mặn đã từng cho rằng ăn chay là thiếu dinh dưỡng. Cuốn sách hướng dẫn bạn làm các món chay từ khai vị đến tráng miệng. Bạn hãy thử chọn một thực đơn cho bữa ăn gia đình mà bạn ưa thích. Sự ngạc nhiên và ngon miệng của mọi người chắc chắn sẽ dành cho bạn. Rồi bạn sẽ làm cho họ “ghiền” ăn chay bởi tài chế biến của bạn qua các món chay nổi tiếng này! ', 5, 5, 7, CAST(N'2025-05-18T09:56:09.8233621' AS DateTime2), N'/uploads/documents/4414973e-e238-4602-8e3e-abaca6669438_📘 Những Món Ăn Chay Nổi Tiếng.docx', N'Published', N'/uploads/images/b2ab3938-589e-4d8c-a975-5498f13801ae_nhasachmienphi-nhung-mon-an-chay-noi-tieng.jpg', 0, NULL, NULL)
INSERT [dbo].[Documents] ([DocumentID], [Title], [Description], [CategoryID], [PublisherID], [AuthorID], [UploadDate], [FilePath], [Status], [ImagePath], [IsPaid], [Price], [UserID]) VALUES (30, N'Văn Hóa Ẩm Thực Ninh Bình', N'Ninh Bình là một trong những tỉnh nằm ở vùng duyên hải thuộc châu thổ sông Hồng, có những nét đặc thù riêng của nền văn minh lúa nước, của văn hoá sông Hồng, trong đó có văn hoá ẩm thực.', 5, 6, 8, CAST(N'2025-05-18T09:57:27.7802447' AS DateTime2), N'/uploads/documents/aa1d8cea-0746-4049-94b5-6ba5bbfafd37_📘 Văn Hóa Ẩm Thực Ninh Bình.docx', N'Published', N'/uploads/images/c00f611c-267e-4a95-8fff-63c9cb6df635_nhasachmienphi-van-hoa-am-thuc-ninh-binh.jpg', 0, NULL, NULL)
INSERT [dbo].[Documents] ([DocumentID], [Title], [Description], [CategoryID], [PublisherID], [AuthorID], [UploadDate], [FilePath], [Status], [ImagePath], [IsPaid], [Price], [UserID]) VALUES (31, N'360 Động Từ Bất Quy Tắc Và 12 Thì Cơ Bản Trong Tiếng Anh', N'Cuốn sách này như một người bạn luôn nhắc nhở bạn dùng chính xác các dạng nguyên thể, quá khứ và phân từ của động từ.', 6, 7, 9, CAST(N'2025-05-18T09:59:03.3316568' AS DateTime2), N'/uploads/documents/0316707a-9eb0-4a11-b8b6-fc48dc14e2a0_360 Động Từ Bất Quy Tắc Và 12 Thì Cơ Bản Trong Tiếng Anh.docx', N'Published', N'/uploads/images/fa38ddf2-b522-4e64-83b3-de3cf246e8cd_nhasachmienphi-360-dong-tu-bat-quy-tac-va-12-thi-co-ban-trong-tieng-anh.jpg', 0, NULL, NULL)
INSERT [dbo].[Documents] ([DocumentID], [Title], [Description], [CategoryID], [PublisherID], [AuthorID], [UploadDate], [FilePath], [Status], [ImagePath], [IsPaid], [Price], [UserID]) VALUES (32, N'3000 Từ Vựng Tiếng Anh Thông Dụng Nhất', N'Từ vựng đóng một vai trò đặc biệt quan trọng, nhất là trong giao tiếp. Nhằm đáp ứng nhu cầu đó chúng tôi xin giới thiệu với bạn đọc cuốn 3000 Từ vựng Tiếng Anh thông dụng nhất.', 6, 7, 9, CAST(N'2025-05-18T10:03:00.4182780' AS DateTime2), N'/uploads/documents/052c8ad2-da86-4b89-b7da-601ed0da4dbb_📘 3000 Từ Vựng Tiếng Anh Thông Dụng Nhất.docx', N'Approved', N'/uploads/images/bcbd841c-85ad-4eb2-aa6c-71c7cf2c88a9.jpg', 0, CAST(0.00 AS Decimal(18, 2)), 6)
INSERT [dbo].[Documents] ([DocumentID], [Title], [Description], [CategoryID], [PublisherID], [AuthorID], [UploadDate], [FilePath], [Status], [ImagePath], [IsPaid], [Price], [UserID]) VALUES (33, N'test1', N'test', 4, 9, 10, CAST(N'2025-05-18T13:33:39.5008053' AS DateTime2), N'/uploads/documents/6e7ccfc9-9ddc-4151-84d7-389058c78801_📘 Văn Hóa Ẩm Thực Ninh Bình.docx', N'Published', N'/uploads/images/141a5936-d021-4583-a6ea-2f56e5748c45.jpg', 1, CAST(200000.00 AS Decimal(18, 2)), 4)
INSERT [dbo].[Documents] ([DocumentID], [Title], [Description], [CategoryID], [PublisherID], [AuthorID], [UploadDate], [FilePath], [Status], [ImagePath], [IsPaid], [Price], [UserID]) VALUES (34, N'hhhhhh', N'ưefewewfef', 4, 8, 10, CAST(N'2025-05-18T14:03:38.6870069' AS DateTime2), N'/uploads/documents/1b267532-a57b-4066-accb-86c437b7a286_📘 Learning Vue.docx', N'Approved', N'/uploads/images/7b760bb2-0bec-45f8-99ad-1b043c9012d6.jpg', 1, CAST(10000.00 AS Decimal(18, 2)), 5)
INSERT [dbo].[Documents] ([DocumentID], [Title], [Description], [CategoryID], [PublisherID], [AuthorID], [UploadDate], [FilePath], [Status], [ImagePath], [IsPaid], [Price], [UserID]) VALUES (35, N'test4', N'tets', 5, 7, 11, CAST(N'2025-05-18T15:05:58.2462609' AS DateTime2), N'/uploads/documents/f9925142-cdc7-48bf-b777-f95bc9215f9d_3000 Từ Vựng Tiếng Anh Thông Dụng Nhất.docx', N'Approved', N'/uploads/images/a023ae35-eb94-4615-92d9-ecd0e957a9db.jpg', 1, CAST(20000.00 AS Decimal(18, 2)), 1)
INSERT [dbo].[Documents] ([DocumentID], [Title], [Description], [CategoryID], [PublisherID], [AuthorID], [UploadDate], [FilePath], [Status], [ImagePath], [IsPaid], [Price], [UserID]) VALUES (36, N'test5', N'1', 4, 8, 11, CAST(N'2025-05-18T15:14:26.6414023' AS DateTime2), N'/uploads/documents/aa7d76ec-92f3-4aca-b6d1-bed1cd5283e4_3000 Từ Vựng Tiếng Anh Thông Dụng Nhất (1).docx', N'Approved', N'/uploads/images/0a937f73-3336-478a-a769-d5a79e8801c1.jpg', 1, CAST(50000.00 AS Decimal(18, 2)), 1)
SET IDENTITY_INSERT [dbo].[Documents] OFF
GO
SET IDENTITY_INSERT [dbo].[DocumentStatistics] ON 

INSERT [dbo].[DocumentStatistics] ([StatisticsID], [DocumentID], [ViewCount], [LastUpdated]) VALUES (25, 25, 19, CAST(N'2025-05-22T14:56:06.3792041' AS DateTime2))
INSERT [dbo].[DocumentStatistics] ([StatisticsID], [DocumentID], [ViewCount], [LastUpdated]) VALUES (26, 26, 28, CAST(N'2025-05-18T08:48:56.9464476' AS DateTime2))
INSERT [dbo].[DocumentStatistics] ([StatisticsID], [DocumentID], [ViewCount], [LastUpdated]) VALUES (27, 27, 8, CAST(N'2025-05-18T11:18:34.9294769' AS DateTime2))
INSERT [dbo].[DocumentStatistics] ([StatisticsID], [DocumentID], [ViewCount], [LastUpdated]) VALUES (28, 28, 9, CAST(N'2025-05-23T23:15:09.5971841' AS DateTime2))
INSERT [dbo].[DocumentStatistics] ([StatisticsID], [DocumentID], [ViewCount], [LastUpdated]) VALUES (29, 32, 56, CAST(N'2025-05-23T23:47:37.6155524' AS DateTime2))
INSERT [dbo].[DocumentStatistics] ([StatisticsID], [DocumentID], [ViewCount], [LastUpdated]) VALUES (30, 31, 24, CAST(N'2025-05-23T23:10:13.6784759' AS DateTime2))
INSERT [dbo].[DocumentStatistics] ([StatisticsID], [DocumentID], [ViewCount], [LastUpdated]) VALUES (31, 33, 18, CAST(N'2025-05-23T23:47:54.2291186' AS DateTime2))
INSERT [dbo].[DocumentStatistics] ([StatisticsID], [DocumentID], [ViewCount], [LastUpdated]) VALUES (32, 34, 7, CAST(N'2025-05-23T22:32:05.7426170' AS DateTime2))
INSERT [dbo].[DocumentStatistics] ([StatisticsID], [DocumentID], [ViewCount], [LastUpdated]) VALUES (33, 30, 10, CAST(N'2025-05-22T14:33:32.2440728' AS DateTime2))
INSERT [dbo].[DocumentStatistics] ([StatisticsID], [DocumentID], [ViewCount], [LastUpdated]) VALUES (34, 35, 8, CAST(N'2025-05-21T19:21:58.3120620' AS DateTime2))
INSERT [dbo].[DocumentStatistics] ([StatisticsID], [DocumentID], [ViewCount], [LastUpdated]) VALUES (35, 36, 17, CAST(N'2025-05-23T23:08:53.5848261' AS DateTime2))
INSERT [dbo].[DocumentStatistics] ([StatisticsID], [DocumentID], [ViewCount], [LastUpdated]) VALUES (36, 29, 3, CAST(N'2025-05-23T22:35:19.0220016' AS DateTime2))
SET IDENTITY_INSERT [dbo].[DocumentStatistics] OFF
GO
SET IDENTITY_INSERT [dbo].[Downloads] ON 

INSERT [dbo].[Downloads] ([DownloadID], [UserID], [DocumentID], [DownloadDate], [DownloadType]) VALUES (36, 3, 32, CAST(N'2025-05-18T10:49:25.4198397' AS DateTime2), N'PDF')
INSERT [dbo].[Downloads] ([DownloadID], [UserID], [DocumentID], [DownloadDate], [DownloadType]) VALUES (37, 4, 32, CAST(N'2025-05-18T11:15:14.4623134' AS DateTime2), N'PDF')
INSERT [dbo].[Downloads] ([DownloadID], [UserID], [DocumentID], [DownloadDate], [DownloadType]) VALUES (38, 4, 32, CAST(N'2025-05-18T11:18:49.8418370' AS DateTime2), N'Original')
INSERT [dbo].[Downloads] ([DownloadID], [UserID], [DocumentID], [DownloadDate], [DownloadType]) VALUES (39, 4, 32, CAST(N'2025-05-18T11:20:10.5002124' AS DateTime2), N'Original')
INSERT [dbo].[Downloads] ([DownloadID], [UserID], [DocumentID], [DownloadDate], [DownloadType]) VALUES (40, 1, 30, CAST(N'2025-05-18T15:06:45.8295789' AS DateTime2), N'Original')
INSERT [dbo].[Downloads] ([DownloadID], [UserID], [DocumentID], [DownloadDate], [DownloadType]) VALUES (41, 8, 32, CAST(N'2025-05-22T13:01:45.6191089' AS DateTime2), N'PDF')
INSERT [dbo].[Downloads] ([DownloadID], [UserID], [DocumentID], [DownloadDate], [DownloadType]) VALUES (42, 8, 31, CAST(N'2025-05-22T13:06:32.1724119' AS DateTime2), N'PDF')
INSERT [dbo].[Downloads] ([DownloadID], [UserID], [DocumentID], [DownloadDate], [DownloadType]) VALUES (43, 8, 30, CAST(N'2025-05-22T14:33:34.6032343' AS DateTime2), N'PDF')
INSERT [dbo].[Downloads] ([DownloadID], [UserID], [DocumentID], [DownloadDate], [DownloadType]) VALUES (44, 9, 31, CAST(N'2025-05-23T22:57:28.8683876' AS DateTime2), N'PDF')
SET IDENTITY_INSERT [dbo].[Downloads] OFF
GO
SET IDENTITY_INSERT [dbo].[Favorites] ON 

INSERT [dbo].[Favorites] ([FavoriteID], [UserID], [DocumentID]) VALUES (28, 1, 32)
INSERT [dbo].[Favorites] ([FavoriteID], [UserID], [DocumentID]) VALUES (30, 1, 33)
INSERT [dbo].[Favorites] ([FavoriteID], [UserID], [DocumentID]) VALUES (31, 1, 35)
INSERT [dbo].[Favorites] ([FavoriteID], [UserID], [DocumentID]) VALUES (26, 3, 31)
INSERT [dbo].[Favorites] ([FavoriteID], [UserID], [DocumentID]) VALUES (27, 3, 32)
INSERT [dbo].[Favorites] ([FavoriteID], [UserID], [DocumentID]) VALUES (29, 4, 32)
INSERT [dbo].[Favorites] ([FavoriteID], [UserID], [DocumentID]) VALUES (33, 8, 25)
INSERT [dbo].[Favorites] ([FavoriteID], [UserID], [DocumentID]) VALUES (32, 8, 31)
INSERT [dbo].[Favorites] ([FavoriteID], [UserID], [DocumentID]) VALUES (34, 9, 33)
SET IDENTITY_INSERT [dbo].[Favorites] OFF
GO
SET IDENTITY_INSERT [dbo].[Menu] ON 

INSERT [dbo].[Menu] ([MenuID], [MenuName], [IsActive], [ControllerName], [ActionName], [Levels], [ParentID], [Link], [MenuOrder], [Position]) VALUES (4, N'Trang chủ', 1, N'Home', N'Index', 1, 0, N'/Home', 1, 1)
INSERT [dbo].[Menu] ([MenuID], [MenuName], [IsActive], [ControllerName], [ActionName], [Levels], [ParentID], [Link], [MenuOrder], [Position]) VALUES (5, N'Tài liệu', 1, N'Document', N'Index', 1, 0, N'/Document', 2, 1)
INSERT [dbo].[Menu] ([MenuID], [MenuName], [IsActive], [ControllerName], [ActionName], [Levels], [ParentID], [Link], [MenuOrder], [Position]) VALUES (8, N'Giới thiệu', 1, N'Home', N'Index', 1, 0, N'/About', 3, 1)
INSERT [dbo].[Menu] ([MenuID], [MenuName], [IsActive], [ControllerName], [ActionName], [Levels], [ParentID], [Link], [MenuOrder], [Position]) VALUES (9, N'Liên hệ', 1, N'Home', N'Index', 1, 0, N'/Contact', 4, 1)
SET IDENTITY_INSERT [dbo].[Menu] OFF
GO
SET IDENTITY_INSERT [dbo].[PasswordResetTokens] ON 

INSERT [dbo].[PasswordResetTokens] ([Id], [UserId], [Token], [ExpiryDate], [IsUsed]) VALUES (1, 1, N'wvVwcxbNwDxgOWlSi4bV_hjcCn5mAhZKwGgQ_aayv7Y', CAST(N'2025-05-14T16:50:53.573' AS DateTime), 1)
INSERT [dbo].[PasswordResetTokens] ([Id], [UserId], [Token], [ExpiryDate], [IsUsed]) VALUES (2, 1, N'Qzn4xa9VDZ-Yzeo_OEF_BWDwbqz5yP9Fam6bFKXNl8Y', CAST(N'2025-05-15T10:15:28.210' AS DateTime), 1)
SET IDENTITY_INSERT [dbo].[PasswordResetTokens] OFF
GO
SET IDENTITY_INSERT [dbo].[Publishers] ON 

INSERT [dbo].[Publishers] ([PublisherID], [PublisherName], [Address], [Phone]) VALUES (1, N'NXB Giáo dục Việt Nam', N'81 Trần Hưng Đạo, Hoàn Kiếm, Hà Nội', N'024 3822 0801')
INSERT [dbo].[Publishers] ([PublisherID], [PublisherName], [Address], [Phone]) VALUES (2, N'NXB Đại học Quốc gia Hà Nội', N'16 Hàng Chuối, Hai Bà Trưng, Hà Nội', N'024 3971 4896')
INSERT [dbo].[Publishers] ([PublisherID], [PublisherName], [Address], [Phone]) VALUES (3, N'NXB Kim Đồng', N'55 Quang Trung, Hai Bà Trưng, Hà Nội', N'024 3943 4730')
INSERT [dbo].[Publishers] ([PublisherID], [PublisherName], [Address], [Phone]) VALUES (4, N'NXB Trẻ', N'161B Lý Chính Thắng, Phường 7, Quận 3, TP. Hồ Chí Minh', N'028 3931 6289')
INSERT [dbo].[Publishers] ([PublisherID], [PublisherName], [Address], [Phone]) VALUES (5, N'NXB Tổng hợp TP. HCM', N'62 Nguyễn Thị Minh Khai, Quận 1, TP. Hồ Chí Minh', N'028 3822 5340')
INSERT [dbo].[Publishers] ([PublisherID], [PublisherName], [Address], [Phone]) VALUES (6, N'NXB Chính trị Quốc gia Sự thật', N'6/86 Duy Tân, Cầu Giấy, Hà Nội', N'024 3822 1574')
INSERT [dbo].[Publishers] ([PublisherID], [PublisherName], [Address], [Phone]) VALUES (7, N'NXB Đại học Sư phạm', N'136 Xuân Thủy, Cầu Giấy, Hà Nội', N'024 3754 7735')
INSERT [dbo].[Publishers] ([PublisherID], [PublisherName], [Address], [Phone]) VALUES (8, N'NXB Thông tin và Truyền thông', N'115 Trần Duy Hưng, Cầu Giấy, Hà Nội', N'024 3556 3456')
INSERT [dbo].[Publishers] ([PublisherID], [PublisherName], [Address], [Phone]) VALUES (9, N'NXB Thanh niên', N'64 Bà Triệu, Hoàn Kiếm, Hà Nội', N'024 6263 1718')
INSERT [dbo].[Publishers] ([PublisherID], [PublisherName], [Address], [Phone]) VALUES (10, N'NXB Hội nhà văn', N'65 Nguyễn Du, Hai Bà Trưng, Hà Nội', N'024 3822 2135')
SET IDENTITY_INSERT [dbo].[Publishers] OFF
GO
SET IDENTITY_INSERT [dbo].[Purchases] ON 

INSERT [dbo].[Purchases] ([PurchaseID], [UserID], [DocumentID], [PurchaseDate], [Amount], [TransactionCode], [Status]) VALUES (17, 3, 26, CAST(N'2025-05-18T08:14:28.083' AS DateTime), CAST(10000.00 AS Decimal(18, 2)), N'831528680701416', N'Completed')
INSERT [dbo].[Purchases] ([PurchaseID], [UserID], [DocumentID], [PurchaseDate], [Amount], [TransactionCode], [Status]) VALUES (18, 3, 26, CAST(N'2025-05-18T08:34:42.913' AS DateTime), CAST(10000.00 AS Decimal(18, 2)), N'WALLET-638831540829138931', N'Completed')
INSERT [dbo].[Purchases] ([PurchaseID], [UserID], [DocumentID], [PurchaseDate], [Amount], [TransactionCode], [Status]) VALUES (19, 3, 27, CAST(N'2025-05-18T09:05:11.277' AS DateTime), CAST(100000.00 AS Decimal(18, 2)), N'WALLET-638831559112772927', N'Completed')
INSERT [dbo].[Purchases] ([PurchaseID], [UserID], [DocumentID], [PurchaseDate], [Amount], [TransactionCode], [Status]) VALUES (20, 3, 28, CAST(N'2025-05-18T09:09:51.073' AS DateTime), CAST(300000.00 AS Decimal(18, 2)), N'WALLET-638831561910724604', N'Completed')
INSERT [dbo].[Purchases] ([PurchaseID], [UserID], [DocumentID], [PurchaseDate], [Amount], [TransactionCode], [Status]) VALUES (21, 4, 27, CAST(N'2025-05-18T11:18:34.780' AS DateTime), CAST(100000.00 AS Decimal(18, 2)), N'WALLET-638831639147794778', N'Completed')
INSERT [dbo].[Purchases] ([PurchaseID], [UserID], [DocumentID], [PurchaseDate], [Amount], [TransactionCode], [Status]) VALUES (22, 3, 35, CAST(N'2025-05-18T15:12:12.980' AS DateTime), CAST(20000.00 AS Decimal(18, 2)), N'WALLET-638831779329803016', N'Completed')
INSERT [dbo].[Purchases] ([PurchaseID], [UserID], [DocumentID], [PurchaseDate], [Amount], [TransactionCode], [Status]) VALUES (23, 3, 36, CAST(N'2025-05-18T15:15:01.660' AS DateTime), CAST(50000.00 AS Decimal(18, 2)), N'WALLET-638831781016599794', N'Completed')
INSERT [dbo].[Purchases] ([PurchaseID], [UserID], [DocumentID], [PurchaseDate], [Amount], [TransactionCode], [Status]) VALUES (24, 3, 34, CAST(N'2025-05-19T14:08:57.327' AS DateTime), CAST(10000.00 AS Decimal(18, 2)), N'WALLET-638832605373282125', N'Completed')
SET IDENTITY_INSERT [dbo].[Purchases] OFF
GO
SET IDENTITY_INSERT [dbo].[Ratings] ON 

INSERT [dbo].[Ratings] ([RatingID], [DocumentID], [UserID], [RatingValue], [RatingDate]) VALUES (11, 26, 3, 5, CAST(N'2025-05-18T08:25:36.4181158' AS DateTime2))
INSERT [dbo].[Ratings] ([RatingID], [DocumentID], [UserID], [RatingValue], [RatingDate]) VALUES (12, 35, 3, 5, CAST(N'2025-05-18T15:12:18.5294625' AS DateTime2))
INSERT [dbo].[Ratings] ([RatingID], [DocumentID], [UserID], [RatingValue], [RatingDate]) VALUES (13, 36, 3, 5, CAST(N'2025-05-19T14:27:53.1954952' AS DateTime2))
INSERT [dbo].[Ratings] ([RatingID], [DocumentID], [UserID], [RatingValue], [RatingDate]) VALUES (14, 32, 8, 5, CAST(N'2025-05-22T13:06:56.6279526' AS DateTime2))
INSERT [dbo].[Ratings] ([RatingID], [DocumentID], [UserID], [RatingValue], [RatingDate]) VALUES (15, 25, 8, 5, CAST(N'2025-05-22T14:38:19.7370134' AS DateTime2))
INSERT [dbo].[Ratings] ([RatingID], [DocumentID], [UserID], [RatingValue], [RatingDate]) VALUES (16, 29, 9, 5, CAST(N'2025-05-23T22:35:21.6430178' AS DateTime2))
INSERT [dbo].[Ratings] ([RatingID], [DocumentID], [UserID], [RatingValue], [RatingDate]) VALUES (17, 33, 9, 4, CAST(N'2025-05-23T23:47:51.3062153' AS DateTime2))
SET IDENTITY_INSERT [dbo].[Ratings] OFF
GO
SET IDENTITY_INSERT [dbo].[Slideshow] ON 

INSERT [dbo].[Slideshow] ([SlideID], [Title], [Description], [ImagePath], [Link], [DisplayOrder], [IsActive], [CreatedDate]) VALUES (1, N'kho tàng tri thức', N'tri thức là chìa khoá của nhân loại', N'/uploads/slideshow/a21c4048-c05a-4ee1-b8ce-dae8e4047a33_rick-and-morty-uy-1920x1080.jpg', N'/trithuc', 1, 1, CAST(N'2025-05-12T23:14:06.0000000' AS DateTime2))
INSERT [dbo].[Slideshow] ([SlideID], [Title], [Description], [ImagePath], [Link], [DisplayOrder], [IsActive], [CreatedDate]) VALUES (2, N'Thư viện là kho báu tri thức của nhân loại, nơi lưu giữ tinh hoa của các thế hệ.', N'Mỗi cuốn sách trong thư viện là một thế giới chờ được khám phá.', N'/uploads/slideshow/e6a95291-b5ab-4750-9d8b-4a7c88e3a1ec_lonely-silent-ufo-night-ze-1920x1080.jpg', N'/Profile', 2, 1, CAST(N'2025-05-13T09:28:30.3781508' AS DateTime2))
SET IDENTITY_INSERT [dbo].[Slideshow] OFF
GO
SET IDENTITY_INSERT [dbo].[SystemConfigs] ON 

INSERT [dbo].[SystemConfigs] ([ConfigID], [ConfigKey], [ConfigValue], [Description]) VALUES (1, N'MaxLoginAttempts', N'5', N'Số lần đăng nhập sai tối đa trước khi khóa tài khoản')
INSERT [dbo].[SystemConfigs] ([ConfigID], [ConfigKey], [ConfigValue], [Description]) VALUES (2, N'LockoutTimeMinutes', N'30', N'Thời gian khóa tài khoản tạm thời (phút)')
INSERT [dbo].[SystemConfigs] ([ConfigID], [ConfigKey], [ConfigValue], [Description]) VALUES (3, N'HomePagePaidDocuments', N'4', N'Số lượng tài liệu có phí hiển thị trên trang chủ')
INSERT [dbo].[SystemConfigs] ([ConfigID], [ConfigKey], [ConfigValue], [Description]) VALUES (4, N'HomePageFreeDocuments', N'4', N'Số lượng tài liệu miễn phí hiển thị trên trang chủ')
INSERT [dbo].[SystemConfigs] ([ConfigID], [ConfigKey], [ConfigValue], [Description]) VALUES (5, N'AuthorCommissionPercent', N'100', N'Số % tiền nhận được cho tác giả khi bán tài liệu')
INSERT [dbo].[SystemConfigs] ([ConfigID], [ConfigKey], [ConfigValue], [Description]) VALUES (6, N'SupportedFileFormats', N'.pdf,.doc,.docx,.xls,.xlsx,.xlsm,.ppt,.pptx,.pptm,.txt,.rtf,.csv,.odt,.ods,.odp,.md,.html,.htm,.xml,.json,.log,.zip,.rar,.7z,.mp3,.mp4,.avi,.png,.jpg,.jpeg,.gif,.bmp,.svg', N'Các định dạng được hỗ trợ')
INSERT [dbo].[SystemConfigs] ([ConfigID], [ConfigKey], [ConfigValue], [Description]) VALUES (7, N'MaxFileSize', N'52428800', N'Kích thước tối đa của tệp có thể tải lên (byte - 50MB)')
INSERT [dbo].[SystemConfigs] ([ConfigID], [ConfigKey], [ConfigValue], [Description]) VALUES (8, N'MaxImageSize', N'5242880', N'Kích thước ảnh tối đa cho phép tải lên (byte - 5MB)')
INSERT [dbo].[SystemConfigs] ([ConfigID], [ConfigKey], [ConfigValue], [Description]) VALUES (9, N'ConvertibleToPdfFormats', N'.doc,.docx,.rtf,.odt,.ppt,.pptx,.odp,.xls,.xlsx,.csv,.ods,.txt,.md,.html,.htm,.xml,.json,.log,.png,.jpg,.jpeg,.gif,.bmp,.svg', N'Các định dạng có thể chuyển sang PDF')
INSERT [dbo].[SystemConfigs] ([ConfigID], [ConfigKey], [ConfigValue], [Description]) VALUES (10, N'PointsForApprovedDocument', N'10000', N'Số điểm cho người dùng khi tải tài liệu lên và được duyệt')
SET IDENTITY_INSERT [dbo].[SystemConfigs] OFF
GO
SET IDENTITY_INSERT [dbo].[UserActivities] ON 

INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (1, 3, CAST(N'2025-05-18T10:52:39.597' AS DateTime), N'Read', 31, NULL, N'Đã đọc tài liệu: 360 Động Từ Bất Quy Tắc Và 12 Thì Cơ Bản Trong Tiếng Anh', NULL)
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (2, 3, CAST(N'2025-05-18T10:54:05.237' AS DateTime), N'Read', 32, NULL, N'Đã đọc tài liệu: 3000 Từ Vựng Tiếng Anh Thông Dụng Nhất', NULL)
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (3, 4, CAST(N'2025-05-18T11:15:02.310' AS DateTime), N'Like', 32, NULL, N'Đã thích tài liệu: 3000 Từ Vựng Tiếng Anh Thông Dụng Nhất', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (4, 4, CAST(N'2025-05-18T11:18:34.810' AS DateTime), N'Purchase', 27, NULL, N'Đã mua tài liệu: Laravel 5 Cookbook Enhance Your Amazing Applications', N'Giá: 100000.00 P')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (5, 4, CAST(N'2025-05-18T11:20:10.557' AS DateTime), N'Download', 32, NULL, N'Đã tải xuống tài liệu: 3000 Từ Vựng Tiếng Anh Thông Dụng Nhất', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (6, 4, CAST(N'2025-05-18T11:20:13.070' AS DateTime), N'Read', 32, NULL, N'Đã đọc tài liệu: 3000 Từ Vựng Tiếng Anh Thông Dụng Nhất', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (7, 4, CAST(N'2025-05-18T11:20:34.430' AS DateTime), N'Comment', 32, 23, N'Đã bình luận tài liệu: 3000 Từ Vựng Tiếng Anh Thông Dụng Nhất', N'rất hay')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (8, 1, CAST(N'2025-05-18T15:05:13.357' AS DateTime), N'Like', 33, NULL, N'Đã thích tài liệu: test1', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (9, 1, CAST(N'2025-05-18T15:06:30.903' AS DateTime), N'Like', 35, NULL, N'Đã thích tài liệu: test4', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (10, 1, CAST(N'2025-05-18T15:06:45.853' AS DateTime), N'Download', 30, NULL, N'Đã tải xuống tài liệu: Văn Hóa Ẩm Thực Ninh Bình', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (11, 1, CAST(N'2025-05-18T15:06:49.757' AS DateTime), N'Read', 30, NULL, N'Đã đọc tài liệu: Văn Hóa Ẩm Thực Ninh Bình', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (12, 1, CAST(N'2025-05-18T15:08:02.393' AS DateTime), N'Read', 32, NULL, N'Đã đọc tài liệu: 3000 Từ Vựng Tiếng Anh Thông Dụng Nhất', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (13, 3, CAST(N'2025-05-18T15:12:13.000' AS DateTime), N'Purchase', 35, NULL, N'Đã mua tài liệu: test4', N'Giá: 20000.00 P')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (14, 3, CAST(N'2025-05-18T15:15:01.667' AS DateTime), N'Purchase', 36, NULL, N'Đã mua tài liệu: test5', N'Giá: 50000.00 P')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (15, 1, CAST(N'2025-05-18T17:02:43.260' AS DateTime), N'Read', 32, NULL, N'Đã đọc tài liệu: 3000 Từ Vựng Tiếng Anh Thông Dụng Nhất', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (16, 3, CAST(N'2025-05-19T14:08:57.417' AS DateTime), N'Purchase', 34, NULL, N'Đã mua tài liệu: hhhhhh', N'Giá: 10000.00 P')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (17, 3, CAST(N'2025-05-19T14:09:30.853' AS DateTime), N'Read', 32, NULL, N'Đã đọc tài liệu: 3000 Từ Vựng Tiếng Anh Thông Dụng Nhất', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (18, 3, CAST(N'2025-05-19T14:24:01.003' AS DateTime), N'Read', 33, NULL, N'Đã đọc tài liệu: test1', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (19, 3, CAST(N'2025-05-19T14:26:27.673' AS DateTime), N'Read', 36, NULL, N'Đã đọc tài liệu: test5', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (20, 3, CAST(N'2025-05-19T14:27:10.263' AS DateTime), N'Read', 31, NULL, N'Đã đọc tài liệu: 360 Động Từ Bất Quy Tắc Và 12 Thì Cơ Bản Trong Tiếng Anh', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (21, 8, CAST(N'2025-05-21T13:46:03.980' AS DateTime), N'Register', NULL, NULL, N'Đăng ký tài khoản mới từ ứng dụng di động', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (22, 8, CAST(N'2025-05-21T17:49:18.127' AS DateTime), N'Login', NULL, NULL, N'Đăng nhập vào ứng dụng di động', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (23, 8, CAST(N'2025-05-22T13:01:45.697' AS DateTime), N'Download', 32, NULL, N'Đã tải xuống tài liệu: 3000 Từ Vựng Tiếng Anh Thông Dụng Nhất', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (24, 8, CAST(N'2025-05-22T13:06:32.290' AS DateTime), N'Download', 31, NULL, N'Đã tải xuống tài liệu: 360 Động Từ Bất Quy Tắc Và 12 Thì Cơ Bản Trong Tiếng Anh', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (25, 8, CAST(N'2025-05-22T13:06:40.287' AS DateTime), N'AddFavorite', NULL, NULL, N'Thêm tài liệu vào danh sách yêu thích: 360 Động Từ Bất Quy Tắc Và 12 Thì Cơ Bản Trong Tiếng Anh', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (26, 8, CAST(N'2025-05-22T13:06:56.677' AS DateTime), N'Rate', NULL, NULL, N'Đánh giá tài liệu: 3000 Từ Vựng Tiếng Anh Thông Dụng Nhất - 5 sao', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (27, 8, CAST(N'2025-05-22T13:07:23.893' AS DateTime), N'Login', NULL, NULL, N'Đăng nhập vào ứng dụng di động', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (28, 8, CAST(N'2025-05-22T14:33:34.777' AS DateTime), N'Download', 30, NULL, N'Đã tải xuống tài liệu: Văn Hóa Ẩm Thực Ninh Bình', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (29, 8, CAST(N'2025-05-22T14:38:19.750' AS DateTime), N'Rate', NULL, NULL, N'Đánh giá tài liệu: Machine Learning Cơ Bản - 5 sao', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (30, 8, CAST(N'2025-05-22T14:38:23.603' AS DateTime), N'AddFavorite', NULL, NULL, N'Thêm tài liệu vào danh sách yêu thích: Machine Learning Cơ Bản', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (31, 9, CAST(N'2025-05-23T22:29:55.147' AS DateTime), N'Register', NULL, NULL, N'Đăng ký tài khoản mới từ ứng dụng di động', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (32, 9, CAST(N'2025-05-23T22:31:22.090' AS DateTime), N'Login', NULL, NULL, N'Đăng nhập vào ứng dụng di động', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (33, 9, CAST(N'2025-05-23T22:31:48.827' AS DateTime), N'Comment', NULL, NULL, N'Bình luận tài liệu 34: ''hi''', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (34, 9, CAST(N'2025-05-23T22:31:52.297' AS DateTime), N'LikeComment', NULL, NULL, N'Thích bình luận 24', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (35, 9, CAST(N'2025-05-23T22:31:52.690' AS DateTime), N'UnlikeComment', NULL, NULL, N'Bỏ thích bình luận 24', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (36, 9, CAST(N'2025-05-23T22:31:53.790' AS DateTime), N'LikeComment', NULL, NULL, N'Thích bình luận 24', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (37, 9, CAST(N'2025-05-23T22:32:11.727' AS DateTime), N'Comment', NULL, NULL, N'Bình luận tài liệu 36: ''hi''', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (38, 9, CAST(N'2025-05-23T22:35:21.650' AS DateTime), N'Rate', NULL, NULL, N'Đánh giá tài liệu: Những Món Ăn Chay Nổi Tiếng - 5 sao', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (39, 9, CAST(N'2025-05-23T22:57:28.880' AS DateTime), N'Download', 31, NULL, N'Đã tải xuống tài liệu: 360 Động Từ Bất Quy Tắc Và 12 Thì Cơ Bản Trong Tiếng Anh', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (40, 9, CAST(N'2025-05-23T23:06:19.473' AS DateTime), N'AddFavorite', NULL, NULL, N'Thêm tài liệu vào danh sách yêu thích: test1', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (41, 9, CAST(N'2025-05-23T23:15:58.603' AS DateTime), N'Login', NULL, NULL, N'Đăng nhập vào ứng dụng di động', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (42, 9, CAST(N'2025-05-23T23:47:31.697' AS DateTime), N'Comment', NULL, NULL, N'Bình luận tài liệu 32: ''7tfu''', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (43, 9, CAST(N'2025-05-23T23:47:34.137' AS DateTime), N'Comment', NULL, NULL, N'Bình luận tài liệu 32: ''mhi''', N'')
INSERT [dbo].[UserActivities] ([ActivityID], [UserID], [ActivityDate], [ActivityType], [DocumentID], [CommentID], [Description], [AdditionalData]) VALUES (44, 9, CAST(N'2025-05-23T23:47:51.310' AS DateTime), N'Rate', NULL, NULL, N'Đánh giá tài liệu: test1 - 4 sao', N'')
SET IDENTITY_INSERT [dbo].[UserActivities] OFF
GO
SET IDENTITY_INSERT [dbo].[Users] ON 

INSERT [dbo].[Users] ([UserID], [Username], [Password], [Email], [FullName], [Role], [Status], [ProfileImage], [LoginAttempts], [LockoutEnd]) VALUES (1, N'zxc', N'jOExwO3e7YO63PNWLkkybNuiiW2PFhaRBdOe+ddXL7w=', N'nguyenvanhieu13102003@gmail.com', N'NGUYEN VAN HIEU', N'User', N'Active', N'd5459e3c-d9c0-455f-a375-dc3d4f6cb6fa.jpg', 0, NULL)
INSERT [dbo].[Users] ([UserID], [Username], [Password], [Email], [FullName], [Role], [Status], [ProfileImage], [LoginAttempts], [LockoutEnd]) VALUES (3, N'admin', N'JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=', N'admin@gmail.com', N'A đờ min', N'Admin', N'Active', N'4a44a131-8bdc-4890-a3d6-02250d9be435.jpg', 0, NULL)
INSERT [dbo].[Users] ([UserID], [Username], [Password], [Email], [FullName], [Role], [Status], [ProfileImage], [LoginAttempts], [LockoutEnd]) VALUES (4, N'qưqw', N'jOExwO3e7YO63PNWLkkybNuiiW2PFhaRBdOe+ddXL7w=', N'nvh@gmail.com', N'hieu dep chai', N'User', N'Active', N'9018f9a8-b944-4f59-bab9-2e95476eb5f9.jpg', 0, NULL)
INSERT [dbo].[Users] ([UserID], [Username], [Password], [Email], [FullName], [Role], [Status], [ProfileImage], [LoginAttempts], [LockoutEnd]) VALUES (5, N'zxcvbnm8', N'jOExwO3e7YO63PNWLkkybNuiiW2PFhaRBdOe+ddXL7w=', N'hieutoptopmusic@gmail.com', N'hiếu', N'User', N'Active', N'smile.jpg', 0, NULL)
INSERT [dbo].[Users] ([UserID], [Username], [Password], [Email], [FullName], [Role], [Status], [ProfileImage], [LoginAttempts], [LockoutEnd]) VALUES (6, N'ngan', N'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', N'ngan@gmail.com', N'Nguyen ngan', N'User', N'Active', N'smile.jpg', 0, NULL)
INSERT [dbo].[Users] ([UserID], [Username], [Password], [Email], [FullName], [Role], [Status], [ProfileImage], [LoginAttempts], [LockoutEnd]) VALUES (7, N'test', N'7NcYcNGWMxapfjrDQIyYNa2M8PPBvHA1J8MCZVNPda4=', N'test@gmail.com', N'Nguyễn Văn Hiếu', N'User', N'Active', N'smile.jpg', 0, NULL)
INSERT [dbo].[Users] ([UserID], [Username], [Password], [Email], [FullName], [Role], [Status], [ProfileImage], [LoginAttempts], [LockoutEnd]) VALUES (8, N'hieunv@gmail.com', N'$2a$11$XAcZlLuT8k9ltj6JA0f9muahWetUvLGno7XMlcOX2ZYdA7F.DMH5W', N'hieunv@gmail.com', N'nguyenvanhieu', N'User', N'Active', N'/uploads/profile/smile.jpg', 0, NULL)
INSERT [dbo].[Users] ([UserID], [Username], [Password], [Email], [FullName], [Role], [Status], [ProfileImage], [LoginAttempts], [LockoutEnd]) VALUES (9, N'hhhhh@gmail.com', N'$2a$11$hldqfz7K1UEUREmogXOQM.DhLLETjYRMN8XDq82M.peYkmPf4L0Aa', N'hhhhh@gmail.com', N'hieu', N'User', N'Active', N'/uploads/profile/smile.jpg', 0, NULL)
SET IDENTITY_INSERT [dbo].[Users] OFF
GO
SET IDENTITY_INSERT [dbo].[Wallets] ON 

INSERT [dbo].[Wallets] ([WalletID], [UserID], [Balance], [CreatedDate], [LastUpdatedDate]) VALUES (3, 1, CAST(490000.00 AS Decimal(18, 2)), CAST(N'2025-05-16T12:08:38.907' AS DateTime), CAST(N'2025-05-18T15:15:01.700' AS DateTime))
INSERT [dbo].[Wallets] ([WalletID], [UserID], [Balance], [CreatedDate], [LastUpdatedDate]) VALUES (4, 3, CAST(9530000.00 AS Decimal(18, 2)), CAST(N'2025-05-16T15:59:49.893' AS DateTime), CAST(N'2025-05-19T14:08:57.390' AS DateTime))
INSERT [dbo].[Wallets] ([WalletID], [UserID], [Balance], [CreatedDate], [LastUpdatedDate]) VALUES (5, 4, CAST(400010.00 AS Decimal(18, 2)), CAST(N'2025-05-18T11:17:47.743' AS DateTime), CAST(N'2025-05-18T13:33:47.823' AS DateTime))
INSERT [dbo].[Wallets] ([WalletID], [UserID], [Balance], [CreatedDate], [LastUpdatedDate]) VALUES (6, 5, CAST(20000.00 AS Decimal(18, 2)), CAST(N'2025-05-18T14:03:56.153' AS DateTime), CAST(N'2025-05-19T14:08:57.543' AS DateTime))
INSERT [dbo].[Wallets] ([WalletID], [UserID], [Balance], [CreatedDate], [LastUpdatedDate]) VALUES (7, 7, CAST(100000000.00 AS Decimal(18, 2)), CAST(N'2025-05-19T14:45:36.943' AS DateTime), CAST(N'2025-05-19T14:46:15.760' AS DateTime))
INSERT [dbo].[Wallets] ([WalletID], [UserID], [Balance], [CreatedDate], [LastUpdatedDate]) VALUES (8, 8, CAST(0.00 AS Decimal(18, 2)), CAST(N'2025-05-21T13:46:03.917' AS DateTime), CAST(N'2025-05-21T13:46:03.920' AS DateTime))
INSERT [dbo].[Wallets] ([WalletID], [UserID], [Balance], [CreatedDate], [LastUpdatedDate]) VALUES (9, 9, CAST(0.00 AS Decimal(18, 2)), CAST(N'2025-05-23T22:29:55.073' AS DateTime), CAST(N'2025-05-23T22:29:55.077' AS DateTime))
SET IDENTITY_INSERT [dbo].[Wallets] OFF
GO
SET IDENTITY_INSERT [dbo].[WalletTransactions] ON 

INSERT [dbo].[WalletTransactions] ([TransactionID], [WalletID], [Amount], [TransactionDate], [Type], [Description], [DocumentID], [PurchaseID]) VALUES (3, 4, CAST(10000000.00 AS Decimal(18, 2)), CAST(N'2025-05-18T08:34:26.217' AS DateTime), N'Credit', N'Nạp tiền vào ví - Mã GD: 14963315', NULL, NULL)
INSERT [dbo].[WalletTransactions] ([TransactionID], [WalletID], [Amount], [TransactionDate], [Type], [Description], [DocumentID], [PurchaseID]) VALUES (4, 4, CAST(10000.00 AS Decimal(18, 2)), CAST(N'2025-05-18T08:34:42.943' AS DateTime), N'Debit', N'Thanh toán tài liệu: dư', 26, 18)
INSERT [dbo].[WalletTransactions] ([TransactionID], [WalletID], [Amount], [TransactionDate], [Type], [Description], [DocumentID], [PurchaseID]) VALUES (5, 4, CAST(100000.00 AS Decimal(18, 2)), CAST(N'2025-05-18T09:05:11.307' AS DateTime), N'Debit', N'Thanh toán tài liệu: test', 27, 19)
INSERT [dbo].[WalletTransactions] ([TransactionID], [WalletID], [Amount], [TransactionDate], [Type], [Description], [DocumentID], [PurchaseID]) VALUES (6, 3, CAST(80000.00 AS Decimal(18, 2)), CAST(N'2025-05-18T09:05:11.373' AS DateTime), N'Credit', N'Nhận tiền từ việc bán tài liệu: test', 27, 19)
INSERT [dbo].[WalletTransactions] ([TransactionID], [WalletID], [Amount], [TransactionDate], [Type], [Description], [DocumentID], [PurchaseID]) VALUES (7, 4, CAST(300000.00 AS Decimal(18, 2)), CAST(N'2025-05-18T09:09:51.090' AS DateTime), N'Debit', N'Thanh toán tài liệu: 11', 28, 20)
INSERT [dbo].[WalletTransactions] ([TransactionID], [WalletID], [Amount], [TransactionDate], [Type], [Description], [DocumentID], [PurchaseID]) VALUES (8, 3, CAST(240000.00 AS Decimal(18, 2)), CAST(N'2025-05-18T09:09:51.140' AS DateTime), N'Credit', N'Nhận tiền từ việc bán tài liệu: 11', 28, 20)
INSERT [dbo].[WalletTransactions] ([TransactionID], [WalletID], [Amount], [TransactionDate], [Type], [Description], [DocumentID], [PurchaseID]) VALUES (9, 5, CAST(500000.00 AS Decimal(18, 2)), CAST(N'2025-05-18T11:18:21.877' AS DateTime), N'Credit', N'Nạp tiền vào ví - Mã GD: 14963418', NULL, NULL)
INSERT [dbo].[WalletTransactions] ([TransactionID], [WalletID], [Amount], [TransactionDate], [Type], [Description], [DocumentID], [PurchaseID]) VALUES (10, 5, CAST(100000.00 AS Decimal(18, 2)), CAST(N'2025-05-18T11:18:34.807' AS DateTime), N'Debit', N'Thanh toán 100,000 POINT cho tài liệu: Laravel 5 Cookbook Enhance Your Amazing Applications', 27, 21)
INSERT [dbo].[WalletTransactions] ([TransactionID], [WalletID], [Amount], [TransactionDate], [Type], [Description], [DocumentID], [PurchaseID]) VALUES (11, 3, CAST(80000.00 AS Decimal(18, 2)), CAST(N'2025-05-18T11:18:34.867' AS DateTime), N'Credit', N'Nhận tiền từ việc bán tài liệu: Laravel 5 Cookbook Enhance Your Amazing Applications', 27, 21)
INSERT [dbo].[WalletTransactions] ([TransactionID], [WalletID], [Amount], [TransactionDate], [Type], [Description], [DocumentID], [PurchaseID]) VALUES (12, 5, CAST(10.00 AS Decimal(18, 2)), CAST(N'2025-05-18T13:33:47.823' AS DateTime), N'Credit', N'Thưởng 10 POINT cho việc đăng tải tài liệu: test1', 33, NULL)
INSERT [dbo].[WalletTransactions] ([TransactionID], [WalletID], [Amount], [TransactionDate], [Type], [Description], [DocumentID], [PurchaseID]) VALUES (13, 6, CAST(10000.00 AS Decimal(18, 2)), CAST(N'2025-05-18T14:03:56.163' AS DateTime), N'Credit', N'Thưởng 10,000 POINT cho việc đăng tải tài liệu: test3', 34, NULL)
INSERT [dbo].[WalletTransactions] ([TransactionID], [WalletID], [Amount], [TransactionDate], [Type], [Description], [DocumentID], [PurchaseID]) VALUES (14, 3, CAST(10000.00 AS Decimal(18, 2)), CAST(N'2025-05-18T15:06:20.037' AS DateTime), N'Credit', N'Thưởng 10,000 POINT cho việc đăng tải tài liệu: test4', 35, NULL)
INSERT [dbo].[WalletTransactions] ([TransactionID], [WalletID], [Amount], [TransactionDate], [Type], [Description], [DocumentID], [PurchaseID]) VALUES (15, 4, CAST(20000.00 AS Decimal(18, 2)), CAST(N'2025-05-18T15:12:13.000' AS DateTime), N'Debit', N'Thanh toán 20,000 POINT cho tài liệu: test4', 35, 22)
INSERT [dbo].[WalletTransactions] ([TransactionID], [WalletID], [Amount], [TransactionDate], [Type], [Description], [DocumentID], [PurchaseID]) VALUES (16, 3, CAST(20000.00 AS Decimal(18, 2)), CAST(N'2025-05-18T15:12:13.047' AS DateTime), N'Credit', N'Nhận tiền từ việc bán tài liệu: test4', 35, 22)
INSERT [dbo].[WalletTransactions] ([TransactionID], [WalletID], [Amount], [TransactionDate], [Type], [Description], [DocumentID], [PurchaseID]) VALUES (17, 4, CAST(20000.00 AS Decimal(18, 2)), CAST(N'2025-05-18T15:12:43.453' AS DateTime), N'Credit', N'Nạp tiền vào ví - Mã GD: 14963682', NULL, NULL)
INSERT [dbo].[WalletTransactions] ([TransactionID], [WalletID], [Amount], [TransactionDate], [Type], [Description], [DocumentID], [PurchaseID]) VALUES (18, 3, CAST(10000.00 AS Decimal(18, 2)), CAST(N'2025-05-18T15:14:41.040' AS DateTime), N'Credit', N'Thưởng 10,000 POINT cho việc đăng tải tài liệu: test5', 36, NULL)
INSERT [dbo].[WalletTransactions] ([TransactionID], [WalletID], [Amount], [TransactionDate], [Type], [Description], [DocumentID], [PurchaseID]) VALUES (19, 4, CAST(50000.00 AS Decimal(18, 2)), CAST(N'2025-05-18T15:15:01.667' AS DateTime), N'Debit', N'Thanh toán 50,000 POINT cho tài liệu: test5', 36, 23)
INSERT [dbo].[WalletTransactions] ([TransactionID], [WalletID], [Amount], [TransactionDate], [Type], [Description], [DocumentID], [PurchaseID]) VALUES (20, 3, CAST(50000.00 AS Decimal(18, 2)), CAST(N'2025-05-18T15:15:01.700' AS DateTime), N'Credit', N'Nhận tiền từ việc bán tài liệu: test5', 36, 23)
INSERT [dbo].[WalletTransactions] ([TransactionID], [WalletID], [Amount], [TransactionDate], [Type], [Description], [DocumentID], [PurchaseID]) VALUES (21, 4, CAST(10000.00 AS Decimal(18, 2)), CAST(N'2025-05-19T14:08:57.390' AS DateTime), N'Debit', N'Thanh toán 10,000 POINT cho tài liệu: hhhhhh', 34, 24)
INSERT [dbo].[WalletTransactions] ([TransactionID], [WalletID], [Amount], [TransactionDate], [Type], [Description], [DocumentID], [PurchaseID]) VALUES (22, 6, CAST(10000.00 AS Decimal(18, 2)), CAST(N'2025-05-19T14:08:57.543' AS DateTime), N'Credit', N'Nhận tiền từ việc bán tài liệu: hhhhhh', 34, 24)
INSERT [dbo].[WalletTransactions] ([TransactionID], [WalletID], [Amount], [TransactionDate], [Type], [Description], [DocumentID], [PurchaseID]) VALUES (23, 7, CAST(100000000.00 AS Decimal(18, 2)), CAST(N'2025-05-19T14:46:15.760' AS DateTime), N'Credit', N'Nạp tiền vào ví - Mã GD: 14965507', NULL, NULL)
SET IDENTITY_INSERT [dbo].[WalletTransactions] OFF
GO
/****** Object:  Index [UQ_CommentLikes_User_Comment]    Script Date: 01-Jun-25 11:24:01 PM ******/
ALTER TABLE [dbo].[CommentLikes] ADD  CONSTRAINT [UQ_CommentLikes_User_Comment] UNIQUE NONCLUSTERED 
(
	[UserID] ASC,
	[CommentID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_CommentLikes_CommentID]    Script Date: 01-Jun-25 11:24:01 PM ******/
CREATE NONCLUSTERED INDEX [IX_CommentLikes_CommentID] ON [dbo].[CommentLikes]
(
	[CommentID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_CommentLikes_UserID]    Script Date: 01-Jun-25 11:24:01 PM ******/
CREATE NONCLUSTERED INDEX [IX_CommentLikes_UserID] ON [dbo].[CommentLikes]
(
	[UserID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Comments_DocumentID]    Script Date: 01-Jun-25 11:24:01 PM ******/
CREATE NONCLUSTERED INDEX [IX_Comments_DocumentID] ON [dbo].[Comments]
(
	[DocumentID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Comments_ParentCommentID]    Script Date: 01-Jun-25 11:24:01 PM ******/
CREATE NONCLUSTERED INDEX [IX_Comments_ParentCommentID] ON [dbo].[Comments]
(
	[ParentCommentID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Comments_UserID]    Script Date: 01-Jun-25 11:24:01 PM ******/
CREATE NONCLUSTERED INDEX [IX_Comments_UserID] ON [dbo].[Comments]
(
	[UserID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Documents_AuthorID]    Script Date: 01-Jun-25 11:24:01 PM ******/
CREATE NONCLUSTERED INDEX [IX_Documents_AuthorID] ON [dbo].[Documents]
(
	[AuthorID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Documents_CategoryID]    Script Date: 01-Jun-25 11:24:01 PM ******/
CREATE NONCLUSTERED INDEX [IX_Documents_CategoryID] ON [dbo].[Documents]
(
	[CategoryID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Documents_PublisherID]    Script Date: 01-Jun-25 11:24:01 PM ******/
CREATE NONCLUSTERED INDEX [IX_Documents_PublisherID] ON [dbo].[Documents]
(
	[PublisherID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Documents_UserID]    Script Date: 01-Jun-25 11:24:01 PM ******/
CREATE NONCLUSTERED INDEX [IX_Documents_UserID] ON [dbo].[Documents]
(
	[UserID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_DocumentSimilarityCache_DocumentIDs]    Script Date: 01-Jun-25 11:24:01 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_DocumentSimilarityCache_DocumentIDs] ON [dbo].[DocumentSimilarityCache]
(
	[DocumentID] ASC,
	[SimilarDocumentID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_DocumentSimilarityCache_ExpiryDate]    Script Date: 01-Jun-25 11:24:01 PM ******/
CREATE NONCLUSTERED INDEX [IX_DocumentSimilarityCache_ExpiryDate] ON [dbo].[DocumentSimilarityCache]
(
	[ExpiryDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_DocumentStatistics_DocumentID]    Script Date: 01-Jun-25 11:24:01 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_DocumentStatistics_DocumentID] ON [dbo].[DocumentStatistics]
(
	[DocumentID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Downloads_DocumentID]    Script Date: 01-Jun-25 11:24:01 PM ******/
CREATE NONCLUSTERED INDEX [IX_Downloads_DocumentID] ON [dbo].[Downloads]
(
	[DocumentID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Downloads_UserID]    Script Date: 01-Jun-25 11:24:01 PM ******/
CREATE NONCLUSTERED INDEX [IX_Downloads_UserID] ON [dbo].[Downloads]
(
	[UserID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Favorites_DocumentID]    Script Date: 01-Jun-25 11:24:01 PM ******/
CREATE NONCLUSTERED INDEX [IX_Favorites_DocumentID] ON [dbo].[Favorites]
(
	[DocumentID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Favorites_UserID_DocumentID]    Script Date: 01-Jun-25 11:24:01 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_Favorites_UserID_DocumentID] ON [dbo].[Favorites]
(
	[UserID] ASC,
	[DocumentID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_PasswordResetTokens_Token]    Script Date: 01-Jun-25 11:24:01 PM ******/
CREATE NONCLUSTERED INDEX [IX_PasswordResetTokens_Token] ON [dbo].[PasswordResetTokens]
(
	[Token] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Ratings_DocumentID]    Script Date: 01-Jun-25 11:24:01 PM ******/
CREATE NONCLUSTERED INDEX [IX_Ratings_DocumentID] ON [dbo].[Ratings]
(
	[DocumentID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Ratings_UserID]    Script Date: 01-Jun-25 11:24:01 PM ******/
CREATE NONCLUSTERED INDEX [IX_Ratings_UserID] ON [dbo].[Ratings]
(
	[UserID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_SystemConfigs_ConfigKey]    Script Date: 01-Jun-25 11:24:01 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_SystemConfigs_ConfigKey] ON [dbo].[SystemConfigs]
(
	[ConfigKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Transactions_UserID]    Script Date: 01-Jun-25 11:24:01 PM ******/
CREATE NONCLUSTERED INDEX [IX_Transactions_UserID] ON [dbo].[Transactions]
(
	[UserID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_UserActivities_ActivityDate]    Script Date: 01-Jun-25 11:24:01 PM ******/
CREATE NONCLUSTERED INDEX [IX_UserActivities_ActivityDate] ON [dbo].[UserActivities]
(
	[ActivityDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_UserActivities_ActivityType]    Script Date: 01-Jun-25 11:24:01 PM ******/
CREATE NONCLUSTERED INDEX [IX_UserActivities_ActivityType] ON [dbo].[UserActivities]
(
	[ActivityType] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_UserActivities_DocumentID]    Script Date: 01-Jun-25 11:24:01 PM ******/
CREATE NONCLUSTERED INDEX [IX_UserActivities_DocumentID] ON [dbo].[UserActivities]
(
	[DocumentID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_UserActivities_UserID]    Script Date: 01-Jun-25 11:24:01 PM ******/
CREATE NONCLUSTERED INDEX [IX_UserActivities_UserID] ON [dbo].[UserActivities]
(
	[UserID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Wallets_UserID]    Script Date: 01-Jun-25 11:24:01 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_Wallets_UserID] ON [dbo].[Wallets]
(
	[UserID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_WalletTransactions_WalletID]    Script Date: 01-Jun-25 11:24:01 PM ******/
CREATE NONCLUSTERED INDEX [IX_WalletTransactions_WalletID] ON [dbo].[WalletTransactions]
(
	[WalletID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Comments] ADD  DEFAULT ((0)) FOR [LikeCount]
GO
ALTER TABLE [dbo].[Documents] ADD  DEFAULT ((0)) FOR [IsPaid]
GO
ALTER TABLE [dbo].[DocumentSimilarityCache] ADD  DEFAULT (getdate()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[DocumentStatistics] ADD  CONSTRAINT [DEFAULT_DocumentStatistics_ViewCount]  DEFAULT ((0)) FOR [ViewCount]
GO
ALTER TABLE [dbo].[Downloads] ADD  DEFAULT ('Original') FOR [DownloadType]
GO
ALTER TABLE [dbo].[PasswordResetTokens] ADD  DEFAULT ((0)) FOR [IsUsed]
GO
ALTER TABLE [dbo].[UserActivities] ADD  DEFAULT (getdate()) FOR [ActivityDate]
GO
ALTER TABLE [dbo].[Users] ADD  DEFAULT ((0)) FOR [LoginAttempts]
GO
ALTER TABLE [dbo].[Wallets] ADD  DEFAULT ((0)) FOR [Balance]
GO
ALTER TABLE [dbo].[CommentLikes]  WITH CHECK ADD  CONSTRAINT [FK_CommentLikes_Comments] FOREIGN KEY([CommentID])
REFERENCES [dbo].[Comments] ([CommentID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[CommentLikes] CHECK CONSTRAINT [FK_CommentLikes_Comments]
GO
ALTER TABLE [dbo].[CommentLikes]  WITH CHECK ADD  CONSTRAINT [FK_CommentLikes_Users] FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([UserID])
GO
ALTER TABLE [dbo].[CommentLikes] CHECK CONSTRAINT [FK_CommentLikes_Users]
GO
ALTER TABLE [dbo].[Comments]  WITH CHECK ADD  CONSTRAINT [FK_Comments_Documents_DocumentID] FOREIGN KEY([DocumentID])
REFERENCES [dbo].[Documents] ([DocumentID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Comments] CHECK CONSTRAINT [FK_Comments_Documents_DocumentID]
GO
ALTER TABLE [dbo].[Comments]  WITH CHECK ADD  CONSTRAINT [FK_Comments_ParentComments] FOREIGN KEY([ParentCommentID])
REFERENCES [dbo].[Comments] ([CommentID])
GO
ALTER TABLE [dbo].[Comments] CHECK CONSTRAINT [FK_Comments_ParentComments]
GO
ALTER TABLE [dbo].[Comments]  WITH CHECK ADD  CONSTRAINT [FK_Comments_Users_UserID] FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([UserID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Comments] CHECK CONSTRAINT [FK_Comments_Users_UserID]
GO
ALTER TABLE [dbo].[Documents]  WITH CHECK ADD  CONSTRAINT [FK_Documents_Authors_AuthorID] FOREIGN KEY([AuthorID])
REFERENCES [dbo].[Authors] ([AuthorID])
GO
ALTER TABLE [dbo].[Documents] CHECK CONSTRAINT [FK_Documents_Authors_AuthorID]
GO
ALTER TABLE [dbo].[Documents]  WITH CHECK ADD  CONSTRAINT [FK_Documents_Categories_CategoryID] FOREIGN KEY([CategoryID])
REFERENCES [dbo].[Categories] ([CategoryID])
GO
ALTER TABLE [dbo].[Documents] CHECK CONSTRAINT [FK_Documents_Categories_CategoryID]
GO
ALTER TABLE [dbo].[Documents]  WITH CHECK ADD  CONSTRAINT [FK_Documents_Publishers_PublisherID] FOREIGN KEY([PublisherID])
REFERENCES [dbo].[Publishers] ([PublisherID])
GO
ALTER TABLE [dbo].[Documents] CHECK CONSTRAINT [FK_Documents_Publishers_PublisherID]
GO
ALTER TABLE [dbo].[Documents]  WITH CHECK ADD  CONSTRAINT [FK_Documents_Users_UserID] FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([UserID])
GO
ALTER TABLE [dbo].[Documents] CHECK CONSTRAINT [FK_Documents_Users_UserID]
GO
ALTER TABLE [dbo].[DocumentSimilarityCache]  WITH CHECK ADD  CONSTRAINT [FK_DocumentSimilarityCache_Document] FOREIGN KEY([DocumentID])
REFERENCES [dbo].[Documents] ([DocumentID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[DocumentSimilarityCache] CHECK CONSTRAINT [FK_DocumentSimilarityCache_Document]
GO
ALTER TABLE [dbo].[DocumentSimilarityCache]  WITH CHECK ADD  CONSTRAINT [FK_DocumentSimilarityCache_SimilarDocument] FOREIGN KEY([SimilarDocumentID])
REFERENCES [dbo].[Documents] ([DocumentID])
GO
ALTER TABLE [dbo].[DocumentSimilarityCache] CHECK CONSTRAINT [FK_DocumentSimilarityCache_SimilarDocument]
GO
ALTER TABLE [dbo].[DocumentStatistics]  WITH CHECK ADD  CONSTRAINT [FK_DocumentStatistics_Documents_DocumentID] FOREIGN KEY([DocumentID])
REFERENCES [dbo].[Documents] ([DocumentID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[DocumentStatistics] CHECK CONSTRAINT [FK_DocumentStatistics_Documents_DocumentID]
GO
ALTER TABLE [dbo].[Downloads]  WITH CHECK ADD  CONSTRAINT [FK_Downloads_Documents_DocumentID] FOREIGN KEY([DocumentID])
REFERENCES [dbo].[Documents] ([DocumentID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Downloads] CHECK CONSTRAINT [FK_Downloads_Documents_DocumentID]
GO
ALTER TABLE [dbo].[Downloads]  WITH CHECK ADD  CONSTRAINT [FK_Downloads_Users_UserID] FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([UserID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Downloads] CHECK CONSTRAINT [FK_Downloads_Users_UserID]
GO
ALTER TABLE [dbo].[Favorites]  WITH CHECK ADD  CONSTRAINT [FK_Favorites_Documents_DocumentID] FOREIGN KEY([DocumentID])
REFERENCES [dbo].[Documents] ([DocumentID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Favorites] CHECK CONSTRAINT [FK_Favorites_Documents_DocumentID]
GO
ALTER TABLE [dbo].[Favorites]  WITH CHECK ADD  CONSTRAINT [FK_Favorites_Users_UserID] FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([UserID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Favorites] CHECK CONSTRAINT [FK_Favorites_Users_UserID]
GO
ALTER TABLE [dbo].[PasswordResetTokens]  WITH CHECK ADD  CONSTRAINT [FK_PasswordResetTokens_Users] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([UserID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[PasswordResetTokens] CHECK CONSTRAINT [FK_PasswordResetTokens_Users]
GO
ALTER TABLE [dbo].[Purchases]  WITH CHECK ADD  CONSTRAINT [FK_Purchases_Documents] FOREIGN KEY([DocumentID])
REFERENCES [dbo].[Documents] ([DocumentID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Purchases] CHECK CONSTRAINT [FK_Purchases_Documents]
GO
ALTER TABLE [dbo].[Purchases]  WITH CHECK ADD  CONSTRAINT [FK_Purchases_Users] FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([UserID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Purchases] CHECK CONSTRAINT [FK_Purchases_Users]
GO
ALTER TABLE [dbo].[Ratings]  WITH CHECK ADD  CONSTRAINT [FK_Ratings_Documents_DocumentID] FOREIGN KEY([DocumentID])
REFERENCES [dbo].[Documents] ([DocumentID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Ratings] CHECK CONSTRAINT [FK_Ratings_Documents_DocumentID]
GO
ALTER TABLE [dbo].[Ratings]  WITH CHECK ADD  CONSTRAINT [FK_Ratings_Users_UserID] FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([UserID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Ratings] CHECK CONSTRAINT [FK_Ratings_Users_UserID]
GO
ALTER TABLE [dbo].[Transactions]  WITH CHECK ADD  CONSTRAINT [FK_Transactions_Users_UserID] FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([UserID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Transactions] CHECK CONSTRAINT [FK_Transactions_Users_UserID]
GO
ALTER TABLE [dbo].[UserActivities]  WITH CHECK ADD  CONSTRAINT [FK_UserActivities_Documents] FOREIGN KEY([DocumentID])
REFERENCES [dbo].[Documents] ([DocumentID])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[UserActivities] CHECK CONSTRAINT [FK_UserActivities_Documents]
GO
ALTER TABLE [dbo].[UserActivities]  WITH CHECK ADD  CONSTRAINT [FK_UserActivities_Users] FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([UserID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[UserActivities] CHECK CONSTRAINT [FK_UserActivities_Users]
GO
ALTER TABLE [dbo].[Wallets]  WITH CHECK ADD  CONSTRAINT [FK_Wallets_Users] FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([UserID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Wallets] CHECK CONSTRAINT [FK_Wallets_Users]
GO
ALTER TABLE [dbo].[WalletTransactions]  WITH CHECK ADD  CONSTRAINT [FK_WalletTransactions_Documents] FOREIGN KEY([DocumentID])
REFERENCES [dbo].[Documents] ([DocumentID])
GO
ALTER TABLE [dbo].[WalletTransactions] CHECK CONSTRAINT [FK_WalletTransactions_Documents]
GO
ALTER TABLE [dbo].[WalletTransactions]  WITH CHECK ADD  CONSTRAINT [FK_WalletTransactions_Purchases] FOREIGN KEY([PurchaseID])
REFERENCES [dbo].[Purchases] ([PurchaseID])
GO
ALTER TABLE [dbo].[WalletTransactions] CHECK CONSTRAINT [FK_WalletTransactions_Purchases]
GO
ALTER TABLE [dbo].[WalletTransactions]  WITH CHECK ADD  CONSTRAINT [FK_WalletTransactions_Wallets] FOREIGN KEY([WalletID])
REFERENCES [dbo].[Wallets] ([WalletID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[WalletTransactions] CHECK CONSTRAINT [FK_WalletTransactions_Wallets]
GO
/****** Object:  StoredProcedure [dbo].[GetSimilarDocuments]    Script Date: 01-Jun-25 11:24:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[GetSimilarDocuments]
    @DocumentID INT,
    @Count INT = 5
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Kiểm tra cache trước
    DECLARE @now DATETIME = GETDATE();
    
    -- Tìm trong cache nếu có và chưa hết hạn
    SELECT TOP (@Count) d.*
    FROM DocumentSimilarityCache c
    INNER JOIN Documents d ON c.SimilarDocumentID = d.DocumentID
    WHERE c.DocumentID = @DocumentID
      AND c.ExpiryDate > @now
      AND (d.Status = 'Approved' OR d.Status = 'Published')
    ORDER BY c.SimilarityScore DESC;
    
    -- Nếu không tìm thấy trong cache, tìm theo từ khóa
    IF @@ROWCOUNT = 0
    BEGIN
        -- Lấy từ khóa của tài liệu hiện tại
        WITH DocumentKeywordsWeighted AS (
            SELECT Keyword, Weight
            FROM DocumentKeywords
            WHERE DocumentID = @DocumentID
        )
        
              -- Tìm tài liệu có chung từ khóa và tính điểm tương đồng
        SELECT TOP (@Count) d.*
        FROM Documents d
        LEFT JOIN DocumentStatistics ds ON d.DocumentID = ds.DocumentID
        INNER JOIN (
            SELECT dk.DocumentID, SUM(dk.Weight * kw.Weight) AS SimilarityScore
            FROM DocumentKeywords dk
            INNER JOIN DocumentKeywordsWeighted kw ON dk.Keyword = kw.Keyword
            WHERE dk.DocumentID != @DocumentID
            GROUP BY dk.DocumentID
        ) s ON d.DocumentID = s.DocumentID
        WHERE d.Status = 'Approved' OR d.Status = 'Published'
        ORDER BY s.SimilarityScore DESC, ds.ViewCount DESC;
    END
END
GO
USE [master]
GO
ALTER DATABASE [SenseLibDB] SET  READ_WRITE 
GO
