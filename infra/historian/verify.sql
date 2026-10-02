-- Run with psql -v ON_ERROR_STOP=1 -f verify.sql after schema.sql as an
-- administrator able to SET ROLE. This transaction always rolls back.
BEGIN;

DO $$
DECLARE
    a text := 'schema_verify_a_' || txid_current()::text;
    b text := 'schema_verify_b_' || txid_current()::text;
    c text := 'schema_verify_c_' || txid_current()::text;
    n integer;
    run_before integer;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM historian.schema_migrations WHERE version = 1) THEN
        RAISE EXCEPTION 'schema version marker missing';
    END IF;
    -- A first complete batch creates one current row and one version.
    PERFORM historian.record_collection(jsonb_build_object(
        'source', a, 'status', 'complete',
        'coverage_timestamp', '2026-10-02 08:00:00',
        'records', jsonb_build_array(jsonb_build_object(
            'entity', 'operation', 'source_record_id', 'op-1',
            'payload', jsonb_build_object('state', 'running'),
            'source_timestamp', '2026-10-02 07:59:00'))));
    SELECT count(*) INTO n FROM historian.record_versions v
    JOIN historian.current_records c USING (record_id)
    JOIN historian.sources s USING (source_id)
    WHERE s.source_key = a;
    IF n <> 1 THEN RAISE EXCEPTION 'first batch did not create one revision'; END IF;

    -- Re-reading the same payload records a run but not another revision.
    PERFORM historian.record_collection(jsonb_build_object(
        'source', a, 'status', 'complete', 'records', jsonb_build_array(
            jsonb_build_object('entity', 'operation', 'source_record_id', 'op-1',
                'payload', jsonb_build_object('state', 'running')))));
    SELECT count(*) INTO n FROM historian.record_versions v
    JOIN historian.current_records c USING (record_id)
    JOIN historian.sources s USING (source_id) WHERE s.source_key = a;
    IF n <> 1 THEN RAISE EXCEPTION 'unchanged reread created a revision'; END IF;

    -- A changed payload advances the revision. The same external ID from a
    -- second source remains independent, and an empty batch deletes nothing.
    PERFORM historian.record_collection(jsonb_build_object(
        'source', a, 'status', 'complete', 'records', jsonb_build_array(
            jsonb_build_object('entity', 'operation', 'source_record_id', 'op-1',
                'payload', jsonb_build_object('state', 'stopped')))));
    PERFORM historian.record_collection(jsonb_build_object(
        'source', b, 'status', 'complete', 'records', jsonb_build_array(
            jsonb_build_object('entity', 'operation', 'source_record_id', 'op-1',
                'payload', jsonb_build_object('state', 'running')))));
    PERFORM historian.record_collection(jsonb_build_object(
        'source', a, 'status', 'complete', 'records', '[]'::jsonb));
    SELECT count(*) INTO n FROM historian.current_records c
    JOIN historian.sources s USING (source_id)
    WHERE s.source_key IN (a, b) AND c.entity = 'operation'
        AND c.source_record_id = 'op-1' AND NOT c.is_deleted;
    IF n <> 2 THEN RAISE EXCEPTION 'source isolation or empty-batch retention failed'; END IF;
    SELECT revision INTO n FROM historian.current_records c
    JOIN historian.sources s USING (source_id) WHERE s.source_key = a;
    IF n <> 2 THEN RAISE EXCEPTION 'changed payload did not advance revision'; END IF;

    -- Explicit tombstone advances once; repeating it is idempotent.
    PERFORM historian.record_collection(jsonb_build_object(
        'source', a, 'status', 'complete', 'records', jsonb_build_array(
            jsonb_build_object('entity', 'operation', 'source_record_id', 'op-1',
                'payload', jsonb_build_object('state', 'stopped'), 'deleted', true))));
    PERFORM historian.record_collection(jsonb_build_object(
        'source', a, 'status', 'complete', 'records', jsonb_build_array(
            jsonb_build_object('entity', 'operation', 'source_record_id', 'op-1',
                'payload', jsonb_build_object('state', 'stopped'), 'deleted', true))));
    SELECT revision INTO n FROM historian.current_records c
    JOIN historian.sources s USING (source_id)
    WHERE s.source_key = a AND c.is_deleted;
    IF n <> 3 THEN RAISE EXCEPTION 'explicit deletion was not idempotent'; END IF;

    -- First-seen tombstones are valid source facts; resurrection is revision 2.
    PERFORM historian.record_collection(jsonb_build_object(
        'source', c, 'status', 'complete', 'records', jsonb_build_array(
            jsonb_build_object('entity', 'stop', 'source_record_id', 'old-stop',
                'payload', jsonb_build_object('state', 'closed'), 'deleted', true))));
    SELECT revision INTO n FROM historian.current_records r
    JOIN historian.sources s USING (source_id)
    WHERE s.source_key = c AND r.is_deleted;
    IF n <> 1 THEN RAISE EXCEPTION 'first-seen tombstone not recorded'; END IF;
    PERFORM historian.record_collection(jsonb_build_object(
        'source', c, 'status', 'complete', 'records', jsonb_build_array(
            jsonb_build_object('entity', 'stop', 'source_record_id', 'old-stop',
                'payload', jsonb_build_object('state', 'open'), 'deleted', false))));
    SELECT revision INTO n FROM historian.current_records r
    JOIN historian.sources s USING (source_id)
    WHERE s.source_key = c AND NOT r.is_deleted;
    IF n <> 2 THEN RAISE EXCEPTION 'resurrection did not advance revision'; END IF;

    -- A failed cycle records failure without touching current or versions.
    PERFORM historian.record_collection(jsonb_build_object(
        'source', a, 'status', 'failed', 'error_text', 'temporary collector error',
        'coverage_timestamp', '2026-10-02 08:05:00'));
    SELECT revision INTO n FROM historian.current_records c
    JOIN historian.sources s USING (source_id) WHERE s.source_key = a;
    IF n <> 3 THEN RAISE EXCEPTION 'failed cycle changed current'; END IF;
    SELECT count(*) INTO n FROM historian.collection_runs r
    JOIN historian.sources s USING (source_id)
    WHERE s.source_key = a AND r.status = 'failed';
    IF n <> 1 THEN RAISE EXCEPTION 'failed cycle did not record status'; END IF;

    -- A bad later item rolls back all work from that collection call.
    SELECT count(*) INTO run_before FROM historian.collection_runs r
    JOIN historian.sources s USING (source_id) WHERE s.source_key = a;
    BEGIN
        PERFORM historian.record_collection(jsonb_build_object(
            'source', a, 'status', 'complete', 'records', jsonb_build_array(
                jsonb_build_object('entity', 'machine', 'source_record_id', 'm-1',
                    'payload', '{}'::jsonb),
                jsonb_build_object('entity', 'bogus', 'source_record_id', 'x',
                    'payload', '{}'::jsonb))));
        RAISE EXCEPTION 'bad batch unexpectedly succeeded';
    EXCEPTION WHEN others THEN
        IF SQLERRM = 'bad batch unexpectedly succeeded' THEN RAISE; END IF;
    END;
    SELECT count(*) INTO n FROM historian.collection_runs r
    JOIN historian.sources s USING (source_id) WHERE s.source_key = a;
    IF n <> run_before THEN RAISE EXCEPTION 'failed call left a run behind'; END IF;
    SELECT count(*) INTO n FROM historian.current_records c
    JOIN historian.sources s USING (source_id)
    WHERE s.source_key = a AND c.entity = 'machine';
    IF n <> 0 THEN RAISE EXCEPTION 'failed call left a current row behind'; END IF;

    -- Coverage and source times have no inferred timezone; local literals survive.
    IF NOT EXISTS (
        SELECT 1 FROM historian.collection_runs r JOIN historian.sources s USING (source_id)
        WHERE s.source_key = a AND r.coverage_timestamp = timestamp '2026-10-02 08:00:00'
    ) THEN RAISE EXCEPTION 'coverage timestamp was not preserved'; END IF;
    BEGIN
        PERFORM historian.record_collection(jsonb_build_object('source', a,
            'status', 'complete', 'coverage_timestamp', '2026-10-02T08:00:00Z',
            'records', '[]'::jsonb));
        RAISE EXCEPTION 'timezone suffix unexpectedly accepted';
    EXCEPTION WHEN others THEN
        IF SQLERRM = 'timezone suffix unexpectedly accepted' THEN RAISE; END IF;
    END;

    -- Even the schema owner cannot rewrite a version through normal DML.
    BEGIN
        UPDATE historian.record_versions SET is_deleted = false
        WHERE version_id IN (SELECT version_id FROM historian.record_versions LIMIT 1);
        RAISE EXCEPTION 'version update unexpectedly succeeded';
    EXCEPTION WHEN others THEN
        IF SQLERRM = 'version update unexpectedly succeeded' THEN RAISE; END IF;
    END;
END;
$$;

-- Role tests use actual PostgreSQL permission checks, including the default
-- PUBLIC function grant that schema.sql explicitly revokes.
SET LOCAL ROLE historian_reader;
DO $$
DECLARE n integer;
BEGIN
    SELECT count(*) INTO n FROM historian.current_records;
    IF n < 2 THEN RAISE EXCEPTION 'reader cannot see current records'; END IF;
    BEGIN
        PERFORM historian.record_collection('{"source":"forbidden","status":"complete","records":[]}'::jsonb);
        RAISE EXCEPTION 'reader could ingest';
    EXCEPTION WHEN insufficient_privilege THEN NULL;
    END;
END;
$$;
RESET ROLE;

SET LOCAL ROLE historian_ingest;
DO $$
DECLARE run_id bigint;
BEGIN
    run_id := historian.record_collection('{"source":"schema_verify_ingest","status":"complete","records":[]}'::jsonb);
    IF run_id IS NULL THEN RAISE EXCEPTION 'ingest function returned no run'; END IF;
    BEGIN
        INSERT INTO historian.record_versions(record_id, run_id, revision, payload, observed_at, is_deleted)
        VALUES (1, 1, 1, '{}'::jsonb, clock_timestamp(), false);
        RAISE EXCEPTION 'ingest could directly append history';
    EXCEPTION WHEN insufficient_privilege THEN NULL;
    END;
    BEGIN
        UPDATE historian.current_records SET revision = 100;
        RAISE EXCEPTION 'ingest could directly update current';
    EXCEPTION WHEN insufficient_privilege THEN NULL;
    END;
END;
$$;
RESET ROLE;

ROLLBACK;
SELECT 'historian verification passed (all test writes rolled back)' AS result;
