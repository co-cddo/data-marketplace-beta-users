CREATE TABLE Organisations (
    OrganisationID INT PRIMARY KEY IDENTITY(1,1),
    OrganisationName VARCHAR(255) NOT NULL,
    OrganisationType VARCHAR(255) NULL,
    Visible BIT NULL,
    Allowed BIT NULL,
    Modified DATETIME NULL,
    ModifiedBy INT NULL,
    RequestId INT NULL,
    FOREIGN KEY (RequestId) REFERENCES OrganisationRequests(OrganisationRequestID)
);
