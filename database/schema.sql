-- Flashcards personal app schema
-- PostgreSQL

CREATE TABLE collections (
    id          SERIAL PRIMARY KEY,
    name        VARCHAR(100) NOT NULL,
    description VARCHAR(255),
    created_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE flashcards (
    id            SERIAL PRIMARY KEY,
    collection_id INTEGER NOT NULL REFERENCES collections(id) ON DELETE CASCADE,
    front         VARCHAR(2000) NOT NULL,
    back          VARCHAR(2000) NOT NULL,
    notes         VARCHAR(2000),
    created_date  TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    -- SM-2 spaced repetition fields
    due_date      TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    interval      INTEGER   DEFAULT 1,
    ef            REAL      DEFAULT 2.5,
    repetitions   INTEGER   DEFAULT 0,
    last_reviewed TIMESTAMP
);
