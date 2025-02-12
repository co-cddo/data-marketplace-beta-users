CREATE TABLE Activities (
    ActivityID INT PRIMARY KEY IDENTITY(1,1),
    ActivityName VARCHAR(255) NOT NULL UNIQUE,
    Description VARCHAR(255)
);
