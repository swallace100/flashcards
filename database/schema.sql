-- Flashcards personal app schema
-- Azure SQL / SQL Server

CREATE TABLE collections (
    id           INT IDENTITY(1,1) PRIMARY KEY,
    name         NVARCHAR(100) NOT NULL,
    description  NVARCHAR(255),
    created_date DATETIME2 DEFAULT GETUTCDATE()
);

CREATE TABLE flashcards (
    id            INT IDENTITY(1,1) PRIMARY KEY,
    collection_id INT NOT NULL REFERENCES collections(id) ON DELETE CASCADE,
    front         NVARCHAR(2000) NOT NULL,
    back          NVARCHAR(2000) NOT NULL,
    notes         NVARCHAR(2000),
    created_date  DATETIME2 DEFAULT GETUTCDATE(),
    -- SM-2 spaced repetition fields
    due_date      DATETIME2 DEFAULT GETUTCDATE(),
    interval      INT       DEFAULT 1,
    ef            REAL      DEFAULT 2.5,
    repetitions   INT       DEFAULT 0,
    last_reviewed DATETIME2
);
