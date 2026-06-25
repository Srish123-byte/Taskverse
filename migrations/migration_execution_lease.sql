-- =====================================================================
-- Migration: Crash-safe lease columns for code_execution_requests
-- Description: Columns to support atomic worker claiming with lease
--              expiry, so a crashed dispatcher/poller doesn't strand
--              a row in Running forever.
-- Author: Developer
-- Date: 2026-06-25
-- =====================================================================

-- =====================================================================
-- PART 1: lease_expires_at
-- When the current claim on this request expires. A worker may
-- reclaim the row once this passes, even if status is still Running,
-- to recover from a crashed worker.
-- =====================================================================
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'code_execution_requests'
        AND column_name = 'lease_expires_at'
    ) THEN
        ALTER TABLE public.code_execution_requests
        ADD COLUMN lease_expires_at timestamptz NULL;

        COMMENT ON COLUMN public.code_execution_requests.lease_expires_at
        IS 'When the current claim on this request expires. A worker may reclaim the row once this passes, even if status is still Running, to recover from a crashed worker.';
    END IF;
END $$;


-- =====================================================================
-- PART 2: claimed_by_instance
-- Identifies the specific worker process instance holding the lease.
-- Distinct from worker_id, which is the logical worker config name
-- (e.g. "dispatch-1") that may run on more than one physical instance.
-- =====================================================================
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'code_execution_requests'
        AND column_name = 'claimed_by_instance'
    ) THEN
        ALTER TABLE public.code_execution_requests
        ADD COLUMN claimed_by_instance varchar(100) NULL;

        COMMENT ON COLUMN public.code_execution_requests.claimed_by_instance
        IS 'Unique identifier of the worker process instance currently holding the lease on this request.';
    END IF;
END $$;


-- =====================================================================
-- PART 3: lease_heartbeat_at
-- Last time the claiming worker confirmed it is still alive and
-- processing this request.
-- =====================================================================
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'code_execution_requests'
        AND column_name = 'lease_heartbeat_at'
    ) THEN
        ALTER TABLE public.code_execution_requests
        ADD COLUMN lease_heartbeat_at timestamptz NULL;

        COMMENT ON COLUMN public.code_execution_requests.lease_heartbeat_at
        IS 'Last time the claiming worker confirmed it is still alive and processing this request.';
    END IF;
END $$;


-- =====================================================================
-- PART 4: index to support the reclaim scan
-- Lets a worker cheaply find expired leases without a full table scan.
-- =====================================================================
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes
        WHERE schemaname = 'public' AND tablename = 'code_execution_requests'
        AND indexname = 'ix_code_execution_requests_lease_expires_at'
    ) THEN
        CREATE INDEX ix_code_execution_requests_lease_expires_at
            ON public.code_execution_requests (lease_expires_at)
            WHERE lease_expires_at IS NOT NULL;
    END IF;
END $$;
