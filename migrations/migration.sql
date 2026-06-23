-- Migration: Add coding_question_id to starter_code and student_code tables
-- Author: Developer
-- Date: 2026-06-23

-- Step 1: Add coding_question_id column to starter_code table
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'starter_code'
          AND column_name = 'coding_question_id'
    ) THEN
        ALTER TABLE public.starter_code
        ADD COLUMN coding_question_id uuid NULL;

        ALTER TABLE public.starter_code
        ADD CONSTRAINT fk_starter_code_coding_question
        FOREIGN KEY (coding_question_id)
        REFERENCES public.coding_question (id)
        ON DELETE SET NULL;

        CREATE INDEX ix_starter_code_coding_question_id
        ON public.starter_code (coding_question_id);
    END IF;
END $$;

-- Step 2: Add coding_question_id column to student_code table
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'student_code'
          AND column_name = 'coding_question_id'
    ) THEN
        ALTER TABLE public.student_code
        ADD COLUMN coding_question_id uuid NULL;

        ALTER TABLE public.student_code
        ADD CONSTRAINT fk_student_code_coding_question
        FOREIGN KEY (coding_question_id)
        REFERENCES public.coding_question (id)
        ON DELETE SET NULL;

        CREATE INDEX ix_student_code_coding_question_id
        ON public.student_code (coding_question_id);
    END IF;
END $$;
