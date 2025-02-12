BEGIN TRY
    -- Start a transaction to ensure all operations succeed or fail together
    BEGIN TRANSACTION;

    -- Step 1: Dynamically identify and drop the constraint on the UserRoleApprovals table
    DECLARE @constraintName NVARCHAR(128);
    DECLARE @dropConstraintSQL NVARCHAR(MAX);

    SELECT @constraintName = name 
    FROM sys.check_constraints 
    WHERE parent_object_id = OBJECT_ID('dbo.UserRoleApprovals')
      AND OBJECT_NAME(parent_object_id) = 'UserRoleApprovals'
      AND OBJECT_NAME(OBJECT_ID) = 'UserRoleApprovals'
      AND COLUMNPROPERTY(parent_object_id, 'ApprovalStatus', 'AllowsNull') IS NOT NULL;

    IF @constraintName IS NOT NULL
    BEGIN
        SET @dropConstraintSQL = 'ALTER TABLE [dbo].[UserRoleApprovals] DROP CONSTRAINT ' + QUOTENAME(@constraintName);
        EXEC sp_executesql @dropConstraintSQL;
    END

    -- Step 2: Ensure the Organisations table exists
    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Organisations' AND xtype='U')
    BEGIN
        CREATE TABLE [dbo].[Organisations] (
            OrganisationID INT PRIMARY KEY IDENTITY(1,1),
            OrganisationName VARCHAR(255) NOT NULL,
            Visible BIT NULL
        );
    END

    -- Step 3: Insert the organisation with OrganisationID = 1 if it doesn't exist
    IF NOT EXISTS (SELECT * FROM [dbo].[Organisations] WHERE OrganisationID = 1)
    BEGIN
        SET IDENTITY_INSERT [dbo].[Organisations] ON;

        INSERT INTO [dbo].[Organisations] (OrganisationID, OrganisationName, Visible)
        VALUES (1, 'Cabinet Office', NULL);

        SET IDENTITY_INSERT [dbo].[Organisations] OFF;
    END

    -- Step 4: Ensure the Domains table exists
    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Domains' AND xtype='U')
    BEGIN
        CREATE TABLE [dbo].[Domains] (
            DomainID INT PRIMARY KEY IDENTITY(1,1),
            DomainName VARCHAR(255) NOT NULL,
            OrganisationID INT NOT NULL,
            OrganisationType VARCHAR(255) NOT NULL,
            OrganisationFormat VARCHAR(255) NOT NULL,
            AllowList BIT NOT NULL,
            Visible BIT NULL,
            DataShareRequestMailboxAddress VARCHAR(255) NULL,
            FOREIGN KEY (OrganisationID) REFERENCES Organisations(OrganisationID)
        );
    END

    -- Step 5: Handle the domain with DomainID = 1
    -- Delete the existing domain with a different ID and reinsert it with DomainID = 1
    IF NOT EXISTS (SELECT * FROM [dbo].[Domains] WHERE DomainName = 'digital.cabinet-office.gov.uk')
    BEGIN
        SET IDENTITY_INSERT [dbo].[Domains] ON;

		INSERT INTO [dbo].[Domains] (DomainID, DomainName, OrganisationID, OrganisationType, OrganisationFormat, AllowList, Visible, DataShareRequestMailboxAddress)
		VALUES (1, 'digital.cabinet-office.gov.uk', 1, 'Central government', 'Ministerial department', 1, NULL, 'elliot.rice@digital.cabinet-office.gov.uk');

		SET IDENTITY_INSERT [dbo].[Domains] OFF;
    END

    

    -- Commit the transaction if everything is successful
    COMMIT TRANSACTION;

END TRY
BEGIN CATCH
    -- Rollback the transaction if an error occurs
    ROLLBACK TRANSACTION;
    -- Rethrow the error to be handled by the calling process
    DECLARE @ErrorMessage NVARCHAR(4000), @ErrorSeverity INT, @ErrorState INT;
    SELECT @ErrorMessage = ERROR_MESSAGE(), @ErrorSeverity = ERROR_SEVERITY(), @ErrorState = ERROR_STATE();
    RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;

-- Step 1: Create the Roles table if it doesn't already exist
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Departments' AND xtype='U')
BEGIN
CREATE TABLE [dbo].[Departments]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [DepartmentName] NVARCHAR(255) NOT NULL, 
    [Active] BIT NOT NULL DEFAULT 1, 
    [Created] DATETIME NOT NULL, 
    [CreatedBy] INT NOT NULL, 
    [Updated] DATETIME NULL, 
    [UpdatedBy] INT NULL
);
END

-- Step 3: Enable IDENTITY_INSERT to allow explicit Id insertion
SET IDENTITY_INSERT [dbo].[Departments] ON;

IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Prime Minister''s Office 10 Downing Street')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES (1,'Prime Minister''s Office 10 Downing Street', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Attorney General''s Office')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(2,'Attorney General''s Office', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Cabinet Office')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(4,'Cabinet Office', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Department for Business & Trade')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(6,'Department for Business & Trade', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Department for Culture, Media & Sport')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(7,'Department for Culture, Media & Sport', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Department for Education')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(8,'Department for Education', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Department for Energy Security & Net Zero')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(9,'Department for Energy Security & Net Zero', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Department for Environment Food & Rural Affairs')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(10,'Department for Environment Food & Rural Affairs', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Department for Science, Innovation & Technology')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(11,'Department for Science, Innovation & Technology', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Department for Transport')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(12,'Department for Transport', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Department for Work & Pensions')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(13,'Department for Work & Pensions', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Department of Health & Social Care')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(14,'Department of Health & Social Care', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Foreign, Commonwealth & Development Office')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(15,'Foreign, Commonwealth & Development Office', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='HM Treasury')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(16,'HM Treasury', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Home Office')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(17,'Home Office', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Ministry of Defence')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(18,'Ministry of Defence', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Ministry of Housing, Communities & Local Government')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(19,'Ministry of Housing, Communities & Local Government', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Ministry of Justice')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(20,'Ministry of Justice', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Northern Ireland Office')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(21,'Northern Ireland Office', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Office of the Advocate General for Scotland')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(22,'Office of the Advocate General for Scotland', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Office of the Leader of the House of Commons')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(23,'Office of the Leader of the House of Commons', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Office of the Leader of the House of Lords')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(24,'Office of the Leader of the House of Lords', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Office of the Secretary of State for Scotland')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(25,'Office of the Secretary of State for Scotland', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='UK Export Finance Wales Office')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(26,'UK Export Finance Wales Office', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='The Charity Commission')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(27,'The Charity Commission', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Competition and Markets Authority')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(28,'Competition and Markets Authority', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Crown Prosecution Service')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(29,'Crown Prosecution Service', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Food Standards Agency')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(30,'Food Standards Agency', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Forestry Commission')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(31,'Forestry Commission', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Government Actuary''s Department')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(32,'Government Actuary''s Department', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Government Legal Department')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(33,'Government Legal Department', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='HM Land Registry')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(34,'HM Land Registry', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='HM Revenue & Customs')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(35,'HM Revenue & Customs', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='NS&I')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(36,'NS&I', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='The National Archives')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(37,'The National Archives', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='National Crime Agency')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(38,'National Crime Agency', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Office of Rail and Road')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(39,'Office of Rail and Road', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Ofgem')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(40,'Ofgem', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Ofqual')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(41,'Ofqual', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Ofsted')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(42,'Ofsted', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Serious Fraud Office')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(43,'Serious Fraud Office', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='Supreme Court of the United Kingdom')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(44,'Supreme Court of the United Kingdom', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='UK Statistics Authority')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(45,'UK Statistics Authority', GETDATE(), 1)
END
IF NOT EXISTS (SELECT * FROM [dbo].[Departments] WHERE [DepartmentName]='The Water Services Regulation Authority')
BEGIN
	INSERT INTO [dbo].[Departments] ([Id], [DepartmentName], [Created], [CreatedBy])
	VALUES(46,'The Water Services Regulation Authority', GETDATE(), 1)
END
IF (SELECT COUNT(*) FROM DepertmentToOrganisations) < 1
BEGIN
	INSERT INTO DepertmentToOrganisations (DepartmentId, OrganisationId) 
	SELECT D.Id, O.OrganisationID
	FROM Organisations O
	INNER JOIN Departments D on D.DepartmentName = O.OrganisationName
END
