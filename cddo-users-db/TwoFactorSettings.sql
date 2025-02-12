CREATE TABLE TwoFactorSettings (
    SettingID INT PRIMARY KEY IDENTITY(1,1),
    ActivityID INT,
    IsTwoFactorRequired BIT NOT NULL,
    InactivityDays INT, -- Nullable, used for activities that require a period of inactivity to trigger 2FA
    FOREIGN KEY (ActivityID) REFERENCES Activities(ActivityID) ON DELETE CASCADE
);
