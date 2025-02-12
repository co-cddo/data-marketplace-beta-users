CREATE TABLE [dbo].[OrganisationRequests]
(
    OrganisationRequestID INT PRIMARY KEY IDENTITY(1,1),
    OrganisationName NVARCHAR(255) NULL,
    OrganisationType NVARCHAR(255) NULL,
    OrganisationFormat NVARCHAR(255) NULL,
    DomainName NVARCHAR(255) NULL,
    UserName NVARCHAR(255) NULL,
    CreatedBy NVARCHAR(255),
    CreatedDate DATETIME,
    UpdatedBy INT,
    UpdatedDate DATETIME,
    Status NVARCHAR(50),
    Reason NVARCHAR(MAX),
    ApprovedBy INT,
    ApprovedDate DATETIME,
    RejectedBy INT,
    RejectedDate DATETIME,
    OrganisationID INT NULL, 
    CONSTRAINT [FK_OrganisationRequests_ToOrganisationId] FOREIGN KEY ([OrganisationID]) REFERENCES [Organisations]([OrganisationID]) 
);
