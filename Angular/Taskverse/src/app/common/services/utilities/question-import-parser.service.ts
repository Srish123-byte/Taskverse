import { Injectable } from '@angular/core';
import * as XLSX from 'xlsx';
import { CreateQuestionRequest } from '../api/assessment-admin.service';

export interface ParsedQuestionImportFile {
  fileName: string;
  questions: CreateQuestionRequest[];
}

type RawCell = string | number | boolean | null | undefined;

@Injectable({ providedIn: 'root' })
export class QuestionImportParserService {
  private readonly headerAliases: Record<string, string[]> = {
    stream: ['stream'],
    subjectId: ['subjectid', 'subject_id', 'subject id'],
    subject: ['subject', 'subjectname', 'subject_name', 'subject name'],
    topicId: ['topicid', 'topic_id', 'topic id'],
    topic: ['topic', 'topicname', 'topic_name', 'topic name'],
    topicTag: ['topictag', 'topic_tag', 'topic tag'],
    questionType: ['questiontype', 'question_type', 'question type'],
    questionText: ['question', 'questions', 'questiontext', 'question_text', 'question text'],
    options: ['options', 'optionjson', 'option_json', 'optionsjson', 'options_json'],
    answer: ['answer'],
    explanation: ['explanation'],
    marks: ['marks'],
    negativeMarks: ['negativemarks', 'negative_marks', 'negative marks'],
    difficultyLevel: ['difficultylevel', 'difficulty_level', 'difficulty level', 'difficulty']
  };

  async parse(file: File): Promise<ParsedQuestionImportFile> {
    const extension = this.getExtension(file.name);
    if (!['csv', 'xls', 'xlsx'].includes(extension)) {
      throw new Error('Unsupported file format. Please upload a .csv, .xls, or .xlsx file.');
    }

    const arrayBuffer = await file.arrayBuffer();
    const workbook = XLSX.read(arrayBuffer, { type: 'array' });
    const firstSheetName = workbook.SheetNames[0];

    if (!firstSheetName) {
      throw new Error('The selected file does not contain any worksheet data.');
    }

    const sheet = workbook.Sheets[firstSheetName];
    const rows = XLSX.utils.sheet_to_json<RawCell[]>(sheet, {
      header: 1,
      defval: '',
      raw: false,
      blankrows: false
    });

    if (rows.length < 2) {
      throw new Error('The selected file must include a header row and at least one question row.');
    }

    const headers = rows[0].map(value => this.normalizeText(this.toCellString(value)));
    const questions: CreateQuestionRequest[] = [];

    for (let rowIndex = 1; rowIndex < rows.length; rowIndex += 1) {
      const row = rows[rowIndex];
      const rowNumber = rowIndex + 1;

      if (this.isEmptyRow(row)) {
        continue;
      }

      questions.push(this.mapRowToQuestion(headers, row, rowNumber));
    }

    if (questions.length === 0) {
      throw new Error('The selected file does not contain any populated question rows.');
    }

    return {
      fileName: file.name,
      questions
    };
  }

  private mapRowToQuestion(headers: string[], row: RawCell[], rowNumber: number): CreateQuestionRequest {
    const getValue = (fieldName: keyof CreateQuestionRequest | 'subject' | 'topic' | 'options'): string => {
      const aliases = this.headerAliases[fieldName];
      const matchedIndex = headers.findIndex(header => aliases.includes(this.normalizeHeader(header)));
      return matchedIndex >= 0 ? this.toCellString(row[matchedIndex]) : '';
    };

    const questionType = this.normalizeQuestionType(getValue('questionType'), rowNumber);
    const options = this.parseOptions(getValue('options'), questionType, rowNumber);
    const subjectId = this.normalizeIdentifier(getValue('subjectId'));
    const topicId = this.normalizeIdentifier(getValue('topicId'));
    const subject = this.normalizeNullableText(getValue('subject'));
    const topic = this.normalizeNullableText(getValue('topic'));

    if (!subjectId && !subject) {
      throw new Error(`Row ${rowNumber}: Subject or SubjectId is required.`);
    }

    if (!topicId && !topic) {
      throw new Error(`Row ${rowNumber}: Topic or TopicId is required.`);
    }

    return {
      stream: this.requireText(getValue('stream'), 'Stream', rowNumber),
      subjectId: subjectId || undefined,
      subject: subject || undefined,
      topicId: topicId || undefined,
      topic: topic || undefined,
      topicTag: this.parseTopicTags(getValue('topicTag'), rowNumber),
      questionType,
      questionText: this.requireText(getValue('questionText'), 'QuestionText', rowNumber),
      options,
      answer: this.requireText(getValue('answer'), 'Answer', rowNumber),
      explanation: this.normalizeNullableText(getValue('explanation')) || undefined,
      marks: this.parseNumber(getValue('marks'), 'Marks', rowNumber),
      negativeMarks: this.parseNumber(getValue('negativeMarks'), 'NegativeMarks', rowNumber),
      difficultyLevel: this.parseDifficultyLevel(getValue('difficultyLevel'), rowNumber),
      sourceRowNumber: rowNumber
    };
  }

  private parseOptions(value: string, questionType: string, rowNumber: number): string[] | undefined {
    const normalizedValue = value.trim();

    if (questionType !== 'mcq') {
      return undefined;
    }

    if (!normalizedValue) {
      throw new Error(`Row ${rowNumber}: Options is required for MCQ questions and must be a JSON array.`);
    }

    let parsedValue: unknown;
    try {
      parsedValue = JSON.parse(normalizedValue);
    } catch {
      throw new Error(`Row ${rowNumber}: Options must be a valid JSON array such as ["A","B","C","D"].`);
    }

    if (!Array.isArray(parsedValue) || parsedValue.length === 0) {
      throw new Error(`Row ${rowNumber}: Options must be a non-empty JSON array for MCQ questions.`);
    }

    return parsedValue.map((option, optionIndex) => {
      const normalizedOption = this.normalizeNullableText(String(option ?? ''));
      if (!normalizedOption) {
        throw new Error(`Row ${rowNumber}: Option ${optionIndex + 1} cannot be empty.`);
      }

      return normalizedOption;
    });
  }

  private parseDifficultyLevel(value: string, rowNumber: number): number {
    const normalized = this.normalizeHeader(value);

    switch (normalized) {
      case '1':
      case 'easy':
        return 1;
      case '2':
      case 'medium':
        return 2;
      case '3':
      case 'hard':
        return 3;
      default:
        throw new Error(`Row ${rowNumber}: DifficultyLevel must be 1, 2, 3, Easy, Medium, or Hard.`);
    }
  }

  private parseTopicTags(value: string, rowNumber: number): string[] {
    const normalizedTags = value
      .split(',')
      .map(tag => this.normalizeNullableText(tag))
      .filter((tag): tag is string => Boolean(tag));

    if (normalizedTags.length === 0) {
      throw new Error(`Row ${rowNumber}: TopicTag is required and must contain at least one tag.`);
    }

    return [...new Set(normalizedTags)];
  }

  private normalizeQuestionType(value: string, rowNumber: number): string {
    const normalized = this.normalizeHeader(value);

    switch (normalized) {
      case 'mcq':
      case 'multiplechoice':
      case 'multiplechoices':
        return 'mcq';
      case 'fillintheblanks':
      case 'fillintheblank':
      case 'fib':
        return 'fill in the blanks';
      default:
        throw new Error(`Row ${rowNumber}: QuestionType must be MCQ or Fill in the Blanks.`);
    }
  }

  private parseNumber(value: string, fieldName: string, rowNumber: number): number {
    const normalizedValue = value.trim();
    if (!normalizedValue) {
      throw new Error(`Row ${rowNumber}: ${fieldName} is required.`);
    }

    const parsedValue = Number(normalizedValue);
    if (!Number.isFinite(parsedValue)) {
      throw new Error(`Row ${rowNumber}: ${fieldName} must be a valid number.`);
    }

    return parsedValue;
  }

  private requireText(value: string, fieldName: string, rowNumber: number): string {
    const normalized = this.normalizeNullableText(value);
    if (!normalized) {
      throw new Error(`Row ${rowNumber}: ${fieldName} is required.`);
    }

    return normalized;
  }

  private normalizeIdentifier(value: string): string | null {
    const normalizedValue = value.trim();
    return normalizedValue.length > 0 ? normalizedValue : null;
  }

  private normalizeNullableText(value: string): string | null {
    const normalized = this.normalizeText(value);
    return normalized.length > 0 ? normalized : null;
  }

  private normalizeText(value: string): string {
    return value
      .replace(/[\u200B-\u200D\uFEFF]/g, '')
      .replace(/\s+/g, ' ')
      .trim();
  }

  private normalizeHeader(value: string): string {
    return this.normalizeText(value)
      .toLowerCase()
      .replace(/[^a-z0-9]/g, '');
  }

  private toCellString(value: RawCell): string {
    return `${value ?? ''}`;
  }

  private isEmptyRow(row: RawCell[]): boolean {
    return row.every(cell => this.toCellString(cell).trim().length === 0);
  }

  private getExtension(fileName: string): string {
    const lastDotIndex = fileName.lastIndexOf('.');
    return lastDotIndex >= 0 ? fileName.slice(lastDotIndex + 1).toLowerCase() : '';
  }
}
