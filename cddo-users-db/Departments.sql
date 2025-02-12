CREATE TABLE [dbo].[Departments]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [DepartmentName] NVARCHAR(255) NOT NULL, 
    [Active] BIT NOT NULL DEFAULT 1, 
    [Created] DATETIME NOT NULL, 
    [CreatedBy] INT NOT NULL, 
    [Updated] DATETIME NULL, 
    [UpdatedBy] INT NULL
)
