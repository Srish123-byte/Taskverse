-- =====================================================================
-- Migration: Execution mode + backpressure counters
-- Description: Distinguishes Run vs Submit on code_execution_requests,
--              and adds a Postgres-backed (not in-memory) counter per
--              mode so the dispatcher can apply backpressure and keep
--              Submit from being starved by Run traffic.
-- Author: Developer
-- Date: 2026-06-25
-- =====================================================================

-- =====================================================================
-- PART 1: execution_mode on code_execution_requests
-- =====================================================================
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'code_execution_requests'
        AND column_name = 'execution_mode'
    ) THEN
        ALTER TABLE public.code_execution_requests
        ADD COLUMN execution_mode varchar(20) NOT NULL DEFAULT 'Run';

        COMMENT ON COLUMN public.code_execution_requests.execution_mode
        IS 'Run | Submit. Run is a practice execution; Submit is a final answer. Each has its own active-execution counter and Submit reserves dedicated node capacity (reserved_final_slots) so it cannot be starved by Run traffic.';
    END IF;
END $$;


-- =====================================================================
-- PART 2: coding_engine_counters
-- One row per execution mode. Maintained (not derived via COUNT(*))
-- so a capacity check at request time is an O(1) row read regardless
-- of how large code_execution_requests grows.
-- =====================================================================
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = 'public' AND table_name = 'coding_engine_counters'
    ) THEN
        CREATE TABLE public.coding_engine_counters (
            counter_key    varchar(30) PRIMARY KEY,
            active_count   integer NOT NULL DEFAULT 0,
            max_active     integer NOT NULL DEFAULT 0,
            modified_at    timestamptz
        );

        COMMENT ON TABLE public.coding_engine_counters
        IS 'Global, Postgres-backed active-execution counters, one row per execution mode. Incremented when a code_execution_request is created, decremented when it reaches a terminal status. Used to decide whether to attempt an inline wait on submission, not to reject requests.';
        COMMENT ON COLUMN public.coding_engine_counters.active_count
        IS 'Current count of requests for this mode that are anywhere in the pipeline (Queued or Running) — not just currently dispatched.';
        COMMENT ON COLUMN public.coding_engine_counters.max_active
        IS 'Threshold above which new requests of this mode skip the inline-wait attempt and go straight to a queued response.';
    END IF;
END $$;


-- =====================================================================
-- SEED DATA: one counter row per mode
-- =====================================================================
INSERT INTO public.coding_engine_counters (counter_key, active_count, max_active, modified_at)
VALUES
    ('run', 0, 50, now()),
    ('submit', 0, 20, now())
ON CONFLICT (counter_key) DO NOTHING;
