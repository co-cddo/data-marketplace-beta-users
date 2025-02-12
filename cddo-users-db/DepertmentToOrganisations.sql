CREATE TABLE [dbo].[DepertmentToOrganisations]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [DepartmentId] INT NOT NULL, 
    [OrganisationId] INT NOT NULL, 
    CONSTRAINT [FK_DepertmentToOrganisations_Organisations] FOREIGN KEY ([OrganisationId]) REFERENCES [Organisations]([OrganisationID]), 
    CONSTRAINT [FK_DepertmentToOrganisations_Departments] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments]([Id])
)
