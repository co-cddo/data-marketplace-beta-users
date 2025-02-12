CREATE TABLE Domains (
    DomainID INT PRIMARY KEY IDENTITY(1,1),
    DomainName VARCHAR(255) NOT NULL UNIQUE,
    OrganisationID INT NOT NULL,
    OrganisationType VARCHAR(255) NULL,
    OrganisationFormat VARCHAR(255) NULL,
    AllowList BIT NOT NULL,
    DataShareRequestMailboxAddress NVARCHAR(255) NULL,
    Visible BIT NULL,
    FOREIGN KEY (OrganisationID) REFERENCES Organisations(OrganisationID)
);

