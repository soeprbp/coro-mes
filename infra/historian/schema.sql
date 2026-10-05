-- CoroMES historian, PostgreSQL 17. Install as a trusted database administrator.
-- Only the ingestion function may write records. Application accounts inherit the
-- NOLOGIN group roles below; they must not own this schema or its functions.

BEGIN;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'historian_ingest') THEN
        CREATE ROLE historian_ingest NOLOGIN;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'historian_reader') THEN
        CREATE ROLE historian_reader NOLOGIN;
    END IF;
END
$$;

CREATE SCHEMA IF NOT EXISTS historian;
REVOKE ALL ON SCHEMA historian FROM PUBLIC;
GRANT USAGE ON SCHEMA historian TO historian_ingest, historian_reader;

-- A durable marker lets backup/restore checks identify the installed contract.
CREATE TABLE IF NOT EXISTS historian.schema_migrations (
    version integer PRIMARY KEY,
    description text NOT NULL,
    applied_at timestamptz NOT NULL DEFAULT clock_timestamp()
);
INSERT INTO historian.schema_migrations(version, description)
VALUES (1, 'generic source-neutral historian')
ON CONFLICT (version) DO NOTHING;

-- A source key names one independently collected feed. It is created on first
-- collection, so no separate writer privilege is needed for registration.
CREATE TABLE IF NOT EXISTS historian.sources (
    source_id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    source_key text NOT NULL UNIQUE CHECK (length(source_key) BETWEEN 1 AND 200)
);

-- coverage_timestamp is the source's local timestamp, if it supplies one. No
-- timezone is inferred. observed_at is the database clock in absolute time.
CREATE TABLE IF NOT EXISTS historian.collection_runs (
    run_id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    source_id bigint NOT NULL REFERENCES historian.sources(source_id),
    observed_at timestamptz NOT NULL,
    coverage_timestamp timestamp without time zone,
    status text NOT NULL CHECK (status IN ('complete', 'failed')),
    error_text text,
    records_seen integer NOT NULL DEFAULT 0 CHECK (records_seen >= 0),
    records_changed integer NOT NULL DEFAULT 0 CHECK (records_changed >= 0),
    CHECK ((status = 'failed' AND error_text IS NOT NULL AND records_seen = 0 AND records_changed = 0)
        OR (status = 'complete' AND error_text IS NULL))
);

-- Only these source-neutral MES entities are accepted. A current row retains
-- an explicit tombstone; records omitted from a batch remain unchanged.
CREATE TABLE IF NOT EXISTS historian.current_records (
    record_id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    source_id bigint NOT NULL REFERENCES historian.sources(source_id),
    entity text NOT NULL CHECK (entity IN ('operation', 'stop', 'machine', 'reason', 'shift')),
    source_record_id text NOT NULL CHECK (length(source_record_id) BETWEEN 1 AND 500),
    payload jsonb NOT NULL CHECK (jsonb_typeof(payload) = 'object'),
    source_timestamp timestamp without time zone,
    first_observed_at timestamptz NOT NULL,
    last_observed_at timestamptz NOT NULL,
    revision integer NOT NULL CHECK (revision > 0),
    is_deleted boolean NOT NULL DEFAULT false,
    UNIQUE (source_id, entity, source_record_id)
);

-- Every meaningful payload/deletion change appends one immutable revision.
-- A changed source timestamp alone updates current metadata, not history.
CREATE TABLE IF NOT EXISTS historian.record_versions (
    version_id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    record_id bigint NOT NULL REFERENCES historian.current_records(record_id),
    run_id bigint NOT NULL REFERENCES historian.collection_runs(run_id),
    revision integer NOT NULL CHECK (revision > 0),
    payload jsonb NOT NULL CHECK (jsonb_typeof(payload) = 'object'),
    source_timestamp timestamp without time zone,
    observed_at timestamptz NOT NULL,
    is_deleted boolean NOT NULL,
    UNIQUE (record_id, revision)
);

CREATE INDEX IF NOT EXISTS record_versions_run_idx ON historian.record_versions(run_id);
CREATE INDEX IF NOT EXISTS collection_runs_source_time_idx ON historian.collection_runs(source_id, observed_at DESC);

CREATE OR REPLACE FUNCTION historian.reject_version_mutation()
RETURNS trigger
LANGUAGE plpgsql
SET search_path = pg_catalog
AS $$
BEGIN
    RAISE EXCEPTION 'historian.record_versions is append-only';
END;
$$;

DROP TRIGGER IF EXISTS record_versions_immutable ON historian.record_versions;
CREATE TRIGGER record_versions_immutable
BEFORE UPDATE OR DELETE ON historian.record_versions
FOR EACH ROW EXECUTE FUNCTION historian.reject_version_mutation();

-- Contract: {"source":"key", "status":"complete"|"failed",
-- "coverage_timestamp":"YYYY-MM-DD HH:MI:SS[.fraction]"|null,
-- "records":[{"entity":"operation|stop|machine|reason|shift",
--             "source_record_id":"id", "payload":{},
--             "source_timestamp":"YYYY-MM-DD HH:MI:SS[.fraction]"|null,
--             "deleted":false}]}
-- Failed calls require error_text and no records. Complete calls require an
-- array (which may be empty). Timestamps reject timezone suffixes explicitly.
-- All changes and the run row commit or roll back together. Callers should
-- report a failed collection with a separate failed call after any SQL error.
CREATE OR REPLACE FUNCTION historian.record_collection(p_batch jsonb)
RETURNS bigint
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, historian
AS $$
DECLARE
    v_source_key text;
    v_source_id bigint;
    v_status text;
    v_coverage timestamp without time zone;
    v_observed timestamptz := clock_timestamp();
    v_run_id bigint;
    v_item jsonb;
    v_entity text;
    v_external_id text;
    v_payload jsonb;
    v_source_timestamp timestamp without time zone;
    v_deleted boolean;
    v_current historian.current_records%ROWTYPE;
    v_record_id bigint;
    v_revision integer;
    v_seen integer := 0;
    v_changed integer := 0;
    v_key text;
    v_keys text[] := ARRAY[]::text[];
BEGIN
    IF p_batch IS NULL OR jsonb_typeof(p_batch) <> 'object' THEN
        RAISE EXCEPTION 'batch must be a JSON object';
    END IF;
    v_source_key := p_batch->>'source';
    v_status := p_batch->>'status';
    IF v_source_key IS NULL OR length(v_source_key) NOT BETWEEN 1 AND 200 THEN
        RAISE EXCEPTION 'source must be 1 to 200 characters';
    END IF;
    IF v_status NOT IN ('complete', 'failed') OR v_status IS NULL THEN
        RAISE EXCEPTION 'status must be complete or failed';
    END IF;
    IF p_batch ? 'coverage_timestamp' AND p_batch->>'coverage_timestamp' IS NOT NULL THEN
        IF (p_batch->>'coverage_timestamp') !~ '^\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}:\d{2}(\.\d{1,6})?$' THEN
            RAISE EXCEPTION 'coverage_timestamp must have no timezone';
        END IF;
        v_coverage := (p_batch->>'coverage_timestamp')::timestamp without time zone;
    END IF;
    IF v_status = 'failed' THEN
        IF NULLIF(p_batch->>'error_text', '') IS NULL
            OR (p_batch ? 'records' AND p_batch->'records' <> '[]'::jsonb) THEN
            RAISE EXCEPTION 'failed collection requires error_text and no records';
        END IF;
    ELSIF jsonb_typeof(p_batch->'records') IS DISTINCT FROM 'array' THEN
        RAISE EXCEPTION 'complete collection requires records array';
    ELSIF jsonb_array_length(p_batch->'records') > 10000 THEN
        RAISE EXCEPTION 'collection exceeds 10000 records';
    END IF;

    -- Serialize runs per source to make revision numbering deterministic.
    INSERT INTO historian.sources(source_key) VALUES (v_source_key)
    ON CONFLICT (source_key) DO NOTHING;
    SELECT source_id INTO STRICT v_source_id
    FROM historian.sources WHERE source_key = v_source_key FOR UPDATE;

    INSERT INTO historian.collection_runs(source_id, observed_at, coverage_timestamp, status, error_text)
    VALUES (v_source_id, v_observed, v_coverage, v_status,
            CASE WHEN v_status = 'failed' THEN p_batch->>'error_text' ELSE NULL END)
    RETURNING run_id INTO v_run_id;
    IF v_status = 'failed' THEN
        RETURN v_run_id;
    END IF;

    FOR v_item IN SELECT value FROM jsonb_array_elements(p_batch->'records') AS items(value) LOOP
        IF jsonb_typeof(v_item) <> 'object' THEN
            RAISE EXCEPTION 'each record must be an object';
        END IF;
        v_entity := v_item->>'entity';
        v_external_id := v_item->>'source_record_id';
        v_payload := v_item->'payload';
        IF v_entity IS NULL OR v_entity NOT IN ('operation', 'stop', 'machine', 'reason', 'shift') THEN
            RAISE EXCEPTION 'unsupported entity: %', v_entity;
        END IF;
        IF v_external_id IS NULL OR length(v_external_id) NOT BETWEEN 1 AND 500 THEN
            RAISE EXCEPTION 'source_record_id must be 1 to 500 characters';
        END IF;
        IF jsonb_typeof(v_payload) IS DISTINCT FROM 'object' THEN
            RAISE EXCEPTION 'payload must be a JSON object';
        END IF;
        IF v_item ? 'deleted' AND jsonb_typeof(v_item->'deleted') <> 'boolean' THEN
            RAISE EXCEPTION 'deleted must be boolean';
        END IF;
        v_deleted := COALESCE((v_item->>'deleted')::boolean, false);
        v_source_timestamp := NULL;
        IF v_item ? 'source_timestamp' AND v_item->>'source_timestamp' IS NOT NULL THEN
            IF (v_item->>'source_timestamp') !~ '^\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}:\d{2}(\.\d{1,6})?$' THEN
                RAISE EXCEPTION 'source_timestamp must have no timezone';
            END IF;
            v_source_timestamp := (v_item->>'source_timestamp')::timestamp without time zone;
        END IF;
        v_key := jsonb_build_array(v_entity, v_external_id)::text;
        IF v_key = ANY(v_keys) THEN
            RAISE EXCEPTION 'duplicate record in collection: %/%', v_entity, v_external_id;
        END IF;
        v_keys := array_append(v_keys, v_key);
        v_seen := v_seen + 1;

        SELECT * INTO v_current FROM historian.current_records
        WHERE source_id = v_source_id AND entity = v_entity AND source_record_id = v_external_id
        FOR UPDATE;
        IF NOT FOUND THEN
            INSERT INTO historian.current_records
                (source_id, entity, source_record_id, payload, source_timestamp,
                 first_observed_at, last_observed_at, revision, is_deleted)
            VALUES (v_source_id, v_entity, v_external_id, v_payload, v_source_timestamp,
                    v_observed, v_observed, 1, v_deleted)
            RETURNING record_id, revision INTO v_record_id, v_revision;
            v_changed := v_changed + 1;
        ELSIF v_current.payload IS DISTINCT FROM v_payload
            OR v_current.is_deleted IS DISTINCT FROM v_deleted THEN
            UPDATE historian.current_records
            SET payload = v_payload, source_timestamp = v_source_timestamp,
                last_observed_at = v_observed, revision = revision + 1,
                is_deleted = v_deleted
            WHERE record_id = v_current.record_id
            RETURNING record_id, revision INTO v_record_id, v_revision;
            v_changed := v_changed + 1;
        ELSE
            UPDATE historian.current_records
            SET source_timestamp = v_source_timestamp, last_observed_at = v_observed
            WHERE record_id = v_current.record_id;
            CONTINUE;
        END IF;
        INSERT INTO historian.record_versions
            (record_id, run_id, revision, payload, source_timestamp, observed_at, is_deleted)
        VALUES (v_record_id, v_run_id, v_revision, v_payload, v_source_timestamp,
                v_observed, v_deleted);
    END LOOP;
    UPDATE historian.collection_runs
    SET records_seen = v_seen, records_changed = v_changed
    WHERE run_id = v_run_id;
    RETURN v_run_id;
END;
$$;

-- PostgreSQL grants EXECUTE on new functions to PUBLIC by default.
REVOKE ALL ON FUNCTION historian.record_collection(jsonb) FROM PUBLIC;
REVOKE ALL ON FUNCTION historian.reject_version_mutation() FROM PUBLIC;
REVOKE ALL ON ALL TABLES IN SCHEMA historian FROM PUBLIC, historian_ingest;
REVOKE ALL ON ALL SEQUENCES IN SCHEMA historian FROM PUBLIC, historian_ingest;
GRANT SELECT ON historian.schema_migrations, historian.sources, historian.collection_runs,
    historian.current_records, historian.record_versions TO historian_reader;
GRANT EXECUTE ON FUNCTION historian.record_collection(jsonb) TO historian_ingest;

COMMIT;
