CREATE TABLE [dbo].[ClientCredentials]
(
	[Id] INT PRIMARY KEY IDENTITY,
	UserId INT,
    OrganisationID INT,
    ClientId NVARCHAR(100),
    ClientSecret NVARCHAR(100),
    Scopes NVARCHAR(250),
    Environment NVARCHAR(10),
    Domain NVARCHAR(250),
    [AppName] NVARCHAR(250) NULL, 
    [Expiration] DATETIME NULL,
    [Status] NVARCHAR(50) DEFAULT 'active',
    FOREIGN KEY (OrganisationID) REFERENCES Organisations(OrganisationID),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
)
