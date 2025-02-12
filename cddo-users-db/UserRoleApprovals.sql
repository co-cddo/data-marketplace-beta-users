CREATE TABLE UserRoleApprovals (
    ApprovalID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    DomainID INT NOT NULL,
    OrganisationID INT NOT NULL,
    RoleID INT NOT NULL,
    ApprovalStatus VARCHAR(50) NOT NULL CONSTRAINT UQ_ApprovalStatus CHECK (ApprovalStatus IN ('Pending', 'Approved', 'Rejected', 'Revoked')),
    ApprovedByUserID INT NULL,  -- nullable to allow for pending approvals
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE(),
    RejectionComment NVARCHAR(MAX) NULL,

    [RequestReason] NVARCHAR(MAX) NULL, 
    CONSTRAINT FK_UserRoleApprovals_Users FOREIGN KEY (UserID) REFERENCES Users(UserID),
    CONSTRAINT FK_UserRoleApprovals_Domains FOREIGN KEY (DomainID) REFERENCES Domains(DomainID),
    CONSTRAINT FK_UserRoleApprovals_Organisations FOREIGN KEY (OrganisationID) REFERENCES Organisations(OrganisationID),
    CONSTRAINT FK_UserRoleApprovals_Roles FOREIGN KEY (RoleID) REFERENCES Roles(RoleID),
    CONSTRAINT FK_UserRoleApprovals_ApprovedBy FOREIGN KEY (ApprovedByUserID) REFERENCES Users(UserID) 
);



