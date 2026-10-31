IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Clinics] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(150) NOT NULL,
    CONSTRAINT [PK_Clinics] PRIMARY KEY ([Id])
);

CREATE TABLE [Doctors] (
    [Id] int NOT NULL IDENTITY,
    [ClinicId] int NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [Specialty] nvarchar(100) NULL,
    CONSTRAINT [PK_Doctors] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Doctors_Clinics_ClinicId] FOREIGN KEY ([ClinicId]) REFERENCES [Clinics] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Patients] (
    [Id] int NOT NULL IDENTITY,
    [ClinicId] int NOT NULL,
    [FullName] nvarchar(200) NOT NULL,
    [DateOfBirth] date NULL,
    [PhoneNumber] nvarchar(20) NULL,
    CONSTRAINT [PK_Patients] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Patients_Clinics_ClinicId] FOREIGN KEY ([ClinicId]) REFERENCES [Clinics] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [DoctorSchedules] (
    [Id] int NOT NULL IDENTITY,
    [DoctorId] int NOT NULL,
    [DayOfWeek] int NOT NULL,
    [StartTime] time NOT NULL,
    [EndTime] time NOT NULL,
    CONSTRAINT [PK_DoctorSchedules] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_DoctorSchedules_ValidTime] CHECK ([EndTime] > [StartTime]),
    CONSTRAINT [FK_DoctorSchedules_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Appointments] (
    [Id] int NOT NULL IDENTITY,
    [PatientId] int NOT NULL,
    [DoctorId] int NOT NULL,
    [StartAt] datetime2 NOT NULL,
    [EndAt] datetime2 NOT NULL,
    [Status] int NOT NULL,
    CONSTRAINT [PK_Appointments] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Appointments_ValidTime] CHECK ([EndAt] > [StartAt]),
    CONSTRAINT [FK_Appointments_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Appointments_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[Clinics]'))
    SET IDENTITY_INSERT [Clinics] ON;
INSERT INTO [Clinics] ([Id], [Name])
VALUES (1, N'My Clinic');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[Clinics]'))
    SET IDENTITY_INSERT [Clinics] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ClinicId', N'Name', N'Specialty') AND [object_id] = OBJECT_ID(N'[Doctors]'))
    SET IDENTITY_INSERT [Doctors] ON;
INSERT INTO [Doctors] ([Id], [ClinicId], [Name], [Specialty])
VALUES (1, 1, N'Dr. Ahmed', N'General');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ClinicId', N'Name', N'Specialty') AND [object_id] = OBJECT_ID(N'[Doctors]'))
    SET IDENTITY_INSERT [Doctors] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'DayOfWeek', N'DoctorId', N'EndTime', N'StartTime') AND [object_id] = OBJECT_ID(N'[DoctorSchedules]'))
    SET IDENTITY_INSERT [DoctorSchedules] ON;
INSERT INTO [DoctorSchedules] ([Id], [DayOfWeek], [DoctorId], [EndTime], [StartTime])
VALUES (1, 6, 1, '20:00:00', '16:00:00'),
(2, 0, 1, '20:00:00', '16:00:00'),
(3, 1, 1, '20:00:00', '16:00:00'),
(4, 2, 1, '20:00:00', '16:00:00'),
(5, 3, 1, '20:00:00', '16:00:00'),
(6, 4, 1, '20:00:00', '16:00:00');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'DayOfWeek', N'DoctorId', N'EndTime', N'StartTime') AND [object_id] = OBJECT_ID(N'[DoctorSchedules]'))
    SET IDENTITY_INSERT [DoctorSchedules] OFF;

CREATE UNIQUE INDEX [IX_Appointments_DoctorId_StartAt] ON [Appointments] ([DoctorId], [StartAt]) WHERE [Status] <> 2;

CREATE INDEX [IX_Appointments_PatientId] ON [Appointments] ([PatientId]);

CREATE INDEX [IX_Doctors_ClinicId] ON [Doctors] ([ClinicId]);

CREATE UNIQUE INDEX [IX_DoctorSchedules_DoctorId_DayOfWeek_StartTime] ON [DoctorSchedules] ([DoctorId], [DayOfWeek], [StartTime]);

CREATE INDEX [IX_Patients_ClinicId] ON [Patients] ([ClinicId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261028210522_InitialCreate', N'9.0.0');

UPDATE [Patients] SET [PhoneNumber] = '01000000000' WHERE [PhoneNumber] IS NULL;

UPDATE [Patients] SET [PhoneNumber] = LEFT([PhoneNumber], 11) WHERE LEN([PhoneNumber]) > 11;

UPDATE [Patients] SET [DateOfBirth] = '2000-01-01' WHERE [DateOfBirth] IS NULL;

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Patients]') AND [c].[name] = N'PhoneNumber');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Patients] DROP CONSTRAINT [' + @var0 + '];');
UPDATE [Patients] SET [PhoneNumber] = N'' WHERE [PhoneNumber] IS NULL;
ALTER TABLE [Patients] ALTER COLUMN [PhoneNumber] nvarchar(11) NOT NULL;
ALTER TABLE [Patients] ADD DEFAULT N'' FOR [PhoneNumber];

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Patients]') AND [c].[name] = N'DateOfBirth');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Patients] DROP CONSTRAINT [' + @var1 + '];');
UPDATE [Patients] SET [DateOfBirth] = '0001-01-01' WHERE [DateOfBirth] IS NULL;
ALTER TABLE [Patients] ALTER COLUMN [DateOfBirth] date NOT NULL;
ALTER TABLE [Patients] ADD DEFAULT '0001-01-01' FOR [DateOfBirth];

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ClinicId', N'Name', N'Specialty') AND [object_id] = OBJECT_ID(N'[Doctors]'))
    SET IDENTITY_INSERT [Doctors] ON;
INSERT INTO [Doctors] ([Id], [ClinicId], [Name], [Specialty])
VALUES (2, 1, N'Dr. Sara', N'Pediatrics');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ClinicId', N'Name', N'Specialty') AND [object_id] = OBJECT_ID(N'[Doctors]'))
    SET IDENTITY_INSERT [Doctors] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'DayOfWeek', N'DoctorId', N'EndTime', N'StartTime') AND [object_id] = OBJECT_ID(N'[DoctorSchedules]'))
    SET IDENTITY_INSERT [DoctorSchedules] ON;
INSERT INTO [DoctorSchedules] ([Id], [DayOfWeek], [DoctorId], [EndTime], [StartTime])
VALUES (7, 0, 2, '14:00:00', '10:00:00'),
(8, 2, 2, '14:00:00', '10:00:00'),
(9, 4, 2, '14:00:00', '10:00:00');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'DayOfWeek', N'DoctorId', N'EndTime', N'StartTime') AND [object_id] = OBJECT_ID(N'[DoctorSchedules]'))
    SET IDENTITY_INSERT [DoctorSchedules] OFF;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261031053904_HardenValidation', N'9.0.0');

COMMIT;
GO

