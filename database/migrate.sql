-- Migration: your_old_db → flashcards
--
-- Prerequisites:
--   1. The old your_old_db database still exists and is accessible
--   2. The new flashcards database already has schema.sql applied
--
-- Run against the NEW database:
--   psql -h localhost -U postgres -d flashcards -f migrate.sql
--
-- Replace YOUR_PASSWORD below with your postgres password before running.

CREATE EXTENSION IF NOT EXISTS dblink;

-- ── Collections ───────────────────────────────────────────────────────────────
-- Bring over id, name, description, created_date; drop creator_id/topic_id/etc.

INSERT INTO collections (id, name, description, created_date)
SELECT id, name, description, created_date
FROM dblink(
    'dbname=your_old_db host=localhost user=YOUR_USERNAME password=YOUR_PASSSWORD',
    'SELECT id, name, description, created_date FROM collections'
) AS t(
    id           integer,
    name         varchar(50),
    description  varchar(100),
    created_date timestamp
);

-- ── Flashcards ────────────────────────────────────────────────────────────────
-- SRS state: prefer user_flashcard_state (per-user progress) over the values
-- stored directly on the flashcard row, falling back when no state exists.

INSERT INTO flashcards (id, collection_id, front, back, notes, created_date,
                        due_date, interval, ef, repetitions, last_reviewed)
SELECT
    id,
    collection_id,
    front,
    back,
    notes,
    created_date,
    COALESCE(ufs_due_date,      f_due_date,    NOW())  AS due_date,
    COALESCE(ufs_interval,      f_interval,    1)      AS interval,
    COALESCE(ufs_ef,            f_ef,          2.5)    AS ef,
    COALESCE(ufs_repetitions,   f_repetitions, 0)      AS repetitions,
    COALESCE(ufs_last_reviewed, f_last_reviewed)       AS last_reviewed
FROM dblink(
    'dbname=your_old_db host=localhost user=postgres password=YOUR_PASSWORD',
    'SELECT f.id, f.collection_id, f.front, f.back, f.notes, f.created_date,
            f.due_date, f.interval, f.ef, f.repetitions, f.last_reviewed,
            ufs.due_date, ufs.interval, ufs.ef, ufs.repetitions, ufs.last_reviewed
     FROM flashcards f
     LEFT JOIN user_flashcard_state ufs ON ufs.flashcard_id = f.id'
) AS t(
    id                integer,
    collection_id     integer,
    front             varchar,
    back              varchar,
    notes             varchar,
    created_date      timestamp,
    f_due_date        timestamp,
    f_interval        integer,
    f_ef              real,
    f_repetitions     integer,
    f_last_reviewed   timestamp,
    ufs_due_date      timestamp,
    ufs_interval      integer,
    ufs_ef            real,
    ufs_repetitions   integer,
    ufs_last_reviewed timestamp
);

-- ── Reset sequences so new inserts don't collide ─────────────────────────────
SELECT setval('collections_id_seq', COALESCE((SELECT MAX(id) FROM collections), 1));
SELECT setval('flashcards_id_seq',  COALESCE((SELECT MAX(id) FROM flashcards),  1));
