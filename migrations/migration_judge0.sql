-- =====================================================================
-- Migration: Judge0 Integration Schema Changes
-- Description: Columns + table for Judge0 code execution engine
-- Based on: feature/code_editor_engine (zip) architecture analysis
-- Author: Developer
-- Date: 2026-06-23
-- =====================================================================

-- =====================================================================
-- PART 1: Add judge0_language_id to coding_languages table
-- Maps our internal languages to Judge0 CE language IDs
-- Reference: https://ce.judge0.com/languages/
-- =====================================================================
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'coding_languages'
        AND column_name = 'judge0_language_id'
    ) THEN
        ALTER TABLE public.coding_languages
        ADD COLUMN judge0_language_id integer NULL;

        COMMENT ON COLUMN public.coding_languages.judge0_language_id
        IS 'Judge0 language ID (e.g. 71=Python 3.8.1, 50=C (GCC 9.2.0), 54=C++ (GCC 9.2.0), 63=JavaScript Node 12.14.0, 74=TypeScript 3.7.4, 62=Java OpenJDK 13.0.1, 51=C# Mono 6.6.0.161)';
    END IF;
END $$;


-- =====================================================================
-- PART 2: code_execution_submissions table
-- Tracks individual Judge0 submissions per test case
-- Enables async batch processing + recovery from failures
-- =====================================================================
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = 'public' AND table_name = 'code_execution_submissions'
    ) THEN
        CREATE TABLE public.code_execution_submissions (
            submission_id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
            code_execution_request_id uuid NOT NULL,
            test_case_id             uuid NOT NULL,
            coding_language_id       uuid,

            -- Judge0 tracking
            judge0_token             varchar(100),
            judge0_status_id         smallint,           -- 1=In Queue, 2=Processing, 3=Accepted, 4=Wrong Answer, 5=TLE, 6=Compile Error, 12=Runtime Error, 13=Internal Error, 14=Exec Format Error
            judge0_status_description varchar(50),
            judge0_submitted_at      timestamptz,
            judge0_completed_at      timestamptz,

            -- Execution results
            stdout                   text,
            stderr                   text,
            compile_output           text,
            exit_code                int,
            time_seconds             numeric(10,4),
            memory_kilobytes         int,

            -- Our evaluation
            passed                   boolean NOT NULL DEFAULT false,
            actual_output            text,
            execution_time_ms        int,

            -- Audit
            created_at               timestamptz NOT NULL DEFAULT now(),
            modified_at              timestamptz,

            -- Constraints
            CONSTRAINT fk_submissions_request
                FOREIGN KEY (code_execution_request_id)
                REFERENCES public.code_execution_requests (code_execution_request_id)
                ON DELETE CASCADE,

            CONSTRAINT fk_submissions_test_case
                FOREIGN KEY (test_case_id)
                REFERENCES public.test_cases (test_case_id)
                ON DELETE CASCADE,

            CONSTRAINT fk_submissions_language
                FOREIGN KEY (coding_language_id)
                REFERENCES public.coding_languages (coding_language_id)
                ON DELETE SET NULL
        );

        -- Indexes for query performance
        CREATE INDEX ix_submissions_request_id
            ON public.code_execution_submissions (code_execution_request_id);

        CREATE INDEX ix_submissions_judge0_token
            ON public.code_execution_submissions (judge0_token);

        CREATE INDEX ix_submissions_judge0_status
            ON public.code_execution_submissions (judge0_status_id)
            WHERE judge0_status_id IN (1, 2);

        -- Comments
        COMMENT ON TABLE public.code_execution_submissions
        IS 'Tracks individual Judge0 submissions per test case for code execution requests. Enables async batch processing, retries, and detailed per-test-case result tracking.';
        COMMENT ON COLUMN public.code_execution_submissions.judge0_token
        IS 'The unique token returned by Judge0 for this submission. Used to poll for results.';
        COMMENT ON COLUMN public.code_execution_submissions.judge0_status_id
        IS 'Judge0 status: 1=In Queue, 2=Processing, 3=Accepted, 4=Wrong Answer, 5=TLE, 6=Compile Error, 12=Runtime Error, 13=Internal Error, 14=Exec Format Error';
        COMMENT ON COLUMN public.code_execution_submissions.passed
        IS 'Whether this test case passed evaluation (computed from Judge0 result + our comparison logic for complex modes).';
    END IF;
END $$;


-- =====================================================================
-- PART 3: Add judge0_submission_token to code_execution_requests
-- Optional: store the batch token if we submit all test cases as one Judge0 batch
-- =====================================================================
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'code_execution_requests'
        AND column_name = 'judge0_batch_token'
    ) THEN
        ALTER TABLE public.code_execution_requests
        ADD COLUMN judge0_batch_token varchar(100) NULL;

        COMMENT ON COLUMN public.code_execution_requests.judge0_batch_token
        IS 'Judge0 batch submission token when submitting all test cases as a single batch.';
    END IF;
END $$;


-- =====================================================================
-- SEED DATA: Mapping our languages to Judge0 CE language IDs
-- Reference: https://ce.judge0.com/languages/
-- =====================================================================
UPDATE public.coding_languages
SET judge0_language_id =
    CASE LOWER(language_code)
        WHEN 'python'      THEN 71   -- Python (3.8.1)
        WHEN 'javascript'  THEN 63   -- JavaScript (Node.js 12.14.0)
        WHEN 'typescript'  THEN 74   -- TypeScript (3.7.4)
        WHEN 'java'        THEN 62   -- Java (OpenJDK 13.0.1)
        WHEN 'csharp'      THEN 51   -- C# (Mono 6.6.0.161)
        WHEN 'cpp'         THEN 54   -- C++ (GCC 9.2.0)
        WHEN 'c'           THEN 50   -- C (GCC 9.2.0)
        WHEN 'ruby'        THEN 72   -- Ruby (2.7.0)
        WHEN 'rust'        THEN 73   -- Rust (1.40.0)
        WHEN 'go'          THEN 60   -- Go (1.13.5)
        WHEN 'php'         THEN 68   -- PHP (7.4.1)
        WHEN 'swift'       THEN 83   -- Swift (5.2.3)
        WHEN 'kotlin'      THEN 78   -- Kotlin (1.3.71)
        WHEN 'r'           THEN 80   -- R (4.0.0)
        WHEN 'dart'        THEN 90   -- Dart (2.19.6)
        WHEN 'scala'       THEN 81   -- Scala (2.13.2)
        ELSE NULL
    END
WHERE judge0_language_id IS NULL;
