-- =====================================================================
-- Migration: Judge0 node registry
-- Description: Tracks individual Judge0 CE instances so the dispatcher
--              can pick a healthy node with available capacity instead
--              of being hardcoded to a single Judge0:BaseUrl.
-- Author: Developer
-- Date: 2026-06-25
-- =====================================================================

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = 'public' AND table_name = 'judge0_nodes'
    ) THEN
        CREATE TABLE public.judge0_nodes (
            id                     uuid PRIMARY KEY DEFAULT gen_random_uuid(),
            base_url               varchar(500) NOT NULL,
            enabled                boolean NOT NULL DEFAULT true,
            health_status          varchar(30) NOT NULL DEFAULT 'Unknown',
            active_slots           integer NOT NULL DEFAULT 0,
            reserved_final_slots   integer NOT NULL DEFAULT 0,
            last_health_check_at   timestamptz,
            cooldown_until         timestamptz,
            created_at             timestamptz NOT NULL DEFAULT now(),
            modified_at            timestamptz,

            CONSTRAINT uq_judge0_nodes_base_url UNIQUE (base_url)
        );

        CREATE INDEX ix_judge0_nodes_enabled_health
            ON public.judge0_nodes (enabled, health_status)
            WHERE enabled = true;

        COMMENT ON TABLE public.judge0_nodes
        IS 'Registry of Judge0 CE instances the dispatcher can submit code to, so the system can run more than one Judge0 box and route around unhealthy or saturated ones.';
        COMMENT ON COLUMN public.judge0_nodes.active_slots
        IS 'Remaining execution capacity currently available on this node. Decremented when a request is dispatched here, incremented back when it finishes or fails to dispatch. Not a count of in-use slots.';
        COMMENT ON COLUMN public.judge0_nodes.reserved_final_slots
        IS 'Portion of active_slots held back exclusively for Submit-mode requests. Run-mode dispatch will not claim this node once active_slots would drop to or below this floor.';
        COMMENT ON COLUMN public.judge0_nodes.health_status
        IS 'Unknown | Healthy | Unhealthy, maintained by the periodic health check worker.';
        COMMENT ON COLUMN public.judge0_nodes.cooldown_until
        IS 'While set and in the future, the health check worker will not re-probe this node and the dispatcher treats it as ineligible.';
    END IF;
END $$;


-- =====================================================================
-- judge0_node_id on code_execution_requests
-- Records which node a request was actually dispatched to, so the
-- poller hits the same node and so the slot can be released back to
-- the right place.
-- =====================================================================
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'code_execution_requests'
        AND column_name = 'judge0_node_id'
    ) THEN
        ALTER TABLE public.code_execution_requests
        ADD COLUMN judge0_node_id uuid NULL;

        ALTER TABLE public.code_execution_requests
        ADD CONSTRAINT fk_code_execution_requests_judge0_node
        FOREIGN KEY (judge0_node_id)
        REFERENCES public.judge0_nodes (id)
        ON DELETE SET NULL;

        CREATE INDEX ix_code_execution_requests_judge0_node_id
            ON public.code_execution_requests (judge0_node_id);

        COMMENT ON COLUMN public.code_execution_requests.judge0_node_id
        IS 'The Judge0 node this request was dispatched to. The poller must read this back to know which node to query.';
    END IF;
END $$;
