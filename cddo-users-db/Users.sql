CREATE TABLE Users (
    UserID INT PRIMARY KEY IDENTITY(1,1),
    Email VARCHAR(255) NOT NULL UNIQUE, -- Specify a length for VARCHAR
    LastLogin DATETIME,
    TwoFactorEnabled BIT DEFAULT 0, -- Indicates whether 2FA is enabled (0 = False, 1 = True)
    TwoFactorSecretKey NVARCHAR(MAX), -- The secret key for 2FA
    BackupCodes NVARCHAR(MAX), -- Encrypted backup codes. JSON array.
    EmailNotification BIT NULL, -- JSON column for storing user preferences, settings, and dismissed messages
    WelcomeNotification BIT NULL, -- JSON column for storing user preferences, settings, and dismissed messages
    OrganisationID INT,
    DomainID INT,
    UserName VARCHAR(255), -- Specify a length for VARCHAR
    Visible BIT NULL,
    FOREIGN KEY (OrganisationID) REFERENCES Organisations(OrganisationID),
    FOREIGN KEY (DomainID) REFERENCES Domains(DomainID)
);
