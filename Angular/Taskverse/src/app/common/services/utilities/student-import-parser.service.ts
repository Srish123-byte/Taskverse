import { Injectable } from '@angular/core';
import * as XLSX from 'xlsx';
import { BulkStudentUploadRow } from '../../models/super-admin.model';

export interface ParsedStudentImportFile {
  fileName: string;
  rows: BulkStudentUploadRow[];
}

type RawCell = string | number | boolean | null | undefined;

@Injectable({ providedIn: 'root' })
export class StudentImportParserService {
  private readonly headerAliases: Record<keyof BulkStudentUploadRow, string[]> = {
    fullName: [
      'full name',
      'student name',
      'candidate name',
      'candidate name student',
      'student'
    ],
    email: [
      'email',
      'email id',
      'email address',
      'mail id',
      'email of student',
      'email of the student'
    ],
    phone: [
      'phone',
      'phone number',
      'phone no',
      'contact no',
      'contact number',
      'mobile',
      'mobile no',
      'mobile number'
    ],
    enrollmentNumber: [
      'enrollment number',
      'usn'
    ],
    collegeId: ['college id'],
    collegeName: ['college', 'college name'],
    classId: ['class id'],
    batchId: ['batch id'],
    className: ['class', 'class name']
  };

  async parse(file: File, scope: 'super-admin' | 'college-admin' = 'super-admin'): Promise<ParsedStudentImportFile> {
    const extension = this.getExtension(file.name);
    if (!['csv', 'xls', 'xlsx'].includes(extension)) {
      throw new Error('Unsupported file format. Please upload a .csv, .xls, or .xlsx file.');
    }

    const workbook = XLSX.read(await file.arrayBuffer(), { type: 'array' });
    const firstSheetName = workbook.SheetNames[0];
    if (!firstSheetName) {
      throw new Error('The selected file does not contain any worksheet data.');
    }

    return this.parseWorkbook(file.name, workbook, scope);
  }

  private parseWorkbook(
    fileName: string,
    workbook: XLSX.WorkBook,
    scope: 'super-admin' | 'college-admin'
  ): ParsedStudentImportFile {
    const parsedRows: BulkStudentUploadRow[] = [];
    const requireCollegeIdentifier = scope === 'super-admin';

    if (workbook.SheetNames.length === 1) {
      const rows = this.readSheet(workbook.Sheets[workbook.SheetNames[0]]);
      return {
        fileName,
        rows: this.parseRows(rows, {
          requireCollegeIdentifier,
          defaultClassName: '',
          supportClassColumn: true
        })
      };
    }

    for (const sheetName of workbook.SheetNames) {
      const rows = this.readSheet(workbook.Sheets[sheetName]);
      const parsedSheetRows = this.parseRows(rows, {
        requireCollegeIdentifier,
        defaultClassName: sheetName.trim(),
        supportClassColumn: false
      });

      parsedRows.push(...parsedSheetRows);
    }

    if (parsedRows.length === 0) {
      throw new Error('The selected workbook does not contain any populated student rows.');
    }

    return {
      fileName,
      rows: parsedRows
    };
  }

  private parseRows(
    rows: RawCell[][],
    options: {
      requireCollegeIdentifier: boolean;
      defaultClassName: string;
      supportClassColumn: boolean;
    }
  ): BulkStudentUploadRow[] {
    if (rows.length === 0) {
      throw new Error('The selected file does not contain any worksheet data.');
    }

    const headerRowIndex = this.findHeaderRowIndex(rows);
    const hasHeaderRow = headerRowIndex >= 0;
    const headerRow = hasHeaderRow ? rows[headerRowIndex] : [];
    const headers = headerRow.map(value => this.getHeaderVariants(this.toCellString(value)));
    const parsedRows: BulkStudentUploadRow[] = [];
    const startRowIndex = hasHeaderRow ? headerRowIndex + 1 : 0;

    for (let rowIndex = startRowIndex; rowIndex < rows.length; rowIndex += 1) {
      const row = rows[rowIndex];
      const rowNumber = rowIndex + 1;

      if (this.isEmptyRow(row)) {
        continue;
      }

      parsedRows.push(
        hasHeaderRow
          ? this.mapRow(headers, row, rowNumber, options)
          : this.mapRowWithoutHeaders(row, rowNumber, options)
      );
    }

    if (parsedRows.length === 0) {
      throw new Error('The selected file does not contain any populated student rows.');
    }

    return parsedRows;
  }

  private mapRow(
    headers: string[][],
    row: RawCell[],
    rowNumber: number,
    options: {
      requireCollegeIdentifier: boolean;
      defaultClassName: string;
      supportClassColumn: boolean;
    }
  ): BulkStudentUploadRow {
    const valueFor = (field: keyof BulkStudentUploadRow): string => {
      const index = this.findHeaderIndex(headers, field);
      return index >= 0 ? this.toCellString(row[index]).trim() : '';
    };

    const className = options.defaultClassName || (options.supportClassColumn ? valueFor('className').trim() : '');
    const resolvedClassName = options.defaultClassName.length > 0 || options.supportClassColumn
      ? this.requireValue(className, 'Class', rowNumber)
      : '';

    return {
      fullName: this.requireValue(valueFor('fullName'), 'FullName', rowNumber),
      email: this.requireValue(valueFor('email'), 'Email', rowNumber),
      phone: this.requireValue(valueFor('phone'), 'Phone', rowNumber),
      enrollmentNumber: valueFor('enrollmentNumber').trim(),
      collegeId: valueFor('collegeId').trim(),
      collegeName: options.requireCollegeIdentifier
        ? this.resolveCollegeIdentifier(valueFor('collegeId'), valueFor('collegeName'), rowNumber).collegeName
        : valueFor('collegeName').trim(),
      classId: valueFor('classId').trim(),
      batchId: valueFor('batchId').trim(),
      className: resolvedClassName
    };
  }

  private resolveCollegeIdentifier(collegeId: string, collegeName: string, rowNumber: number): { collegeName: string } {
    const normalizedCollegeId = collegeId.trim();
    const normalizedCollegeName = collegeName.trim();

    if (normalizedCollegeId.length === 0 && normalizedCollegeName.length === 0) {
      throw new Error(`Row ${rowNumber}: CollegeId or College Name is required.`);
    }

    return {
      collegeName: normalizedCollegeName
    };
  }

  private mapRowWithoutHeaders(
    row: RawCell[],
    rowNumber: number,
    options: {
      requireCollegeIdentifier: boolean;
      defaultClassName: string;
      supportClassColumn: boolean;
    }
  ): BulkStudentUploadRow {
    if (options.requireCollegeIdentifier) {
      throw new Error('Header row is required for super admin bulk upload so College Name or collegeId can be resolved.');
    }

    const cells = row.map(value => this.toCellString(value).trim());
    const emailIndex = cells.findIndex(value => this.looksLikeEmail(value));

    if (emailIndex < 0) {
      throw new Error(`Row ${rowNumber}: Email is required.`);
    }

    const phoneIndex = cells.findIndex((value, index) => index !== emailIndex && this.looksLikePhone(value));
    if (phoneIndex < 0) {
      throw new Error(`Row ${rowNumber}: Phone is required.`);
    }

    const enrollmentIndex = emailIndex >= 3 ? 0 : -1;
    const fullNameIndex = enrollmentIndex >= 0 ? 1 : 0;

    const fullName = cells[fullNameIndex] ?? '';
    const email = cells[emailIndex] ?? '';
    const phone = cells[phoneIndex] ?? '';
    const enrollmentNumber = enrollmentIndex >= 0 ? (cells[enrollmentIndex] ?? '') : '';

    const className = options.defaultClassName
      || (options.supportClassColumn ? this.resolveClassNameFromHeaderlessRow(cells, rowNumber, emailIndex) : '');

    return {
      fullName: this.requireValue(fullName, 'FullName', rowNumber),
      email: this.requireValue(email, 'Email', rowNumber),
      phone: this.requireValue(phone, 'Phone', rowNumber),
      enrollmentNumber,
      collegeId: '',
      collegeName: '',
      classId: '',
      batchId: '',
      className: options.defaultClassName.length > 0 || options.supportClassColumn
        ? this.requireValue(className, 'Class', rowNumber)
        : ''
    };
  }

  private readSheet(sheet: XLSX.WorkSheet): RawCell[][] {
    return XLSX.utils.sheet_to_json<RawCell[]>(sheet, {
      header: 1,
      defval: '',
      raw: false,
      blankrows: false
    });
  }

  private requireValue(value: string, fieldName: string, rowNumber: number): string {
    if (value.trim().length === 0) {
      throw new Error(`Row ${rowNumber}: ${fieldName} is required.`);
    }

    return value.trim();
  }

  private getExtension(fileName: string): string {
    const segments = fileName.split('.');
    return segments.length > 1 ? segments.pop()!.toLowerCase() : '';
  }

  private normalizeHeader(value: string): string {
    return value
      .trim()
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, ' ')
      .replace(/\s+/g, ' ')
      .trim();
  }

  private getHeaderVariants(value: string): string[] {
    const normalizedValue = value.trim().toLowerCase();
    const variants = new Set<string>();
    const addVariant = (candidate: string): void => {
      const normalizedCandidate = this.normalizeHeader(candidate);
      if (normalizedCandidate.length > 0) {
        variants.add(normalizedCandidate);
      }
    };

    addVariant(normalizedValue);

    normalizedValue
      .split(/[\/+|,&]+/g)
      .forEach(segment => addVariant(segment));

    return [...variants];
  }

  private findHeaderRowIndex(rows: RawCell[][]): number {
    const rowsToInspect = Math.min(rows.length, 5);
    let bestRowIndex = -1;
    let bestScore = 0;

    for (let rowIndex = 0; rowIndex < rowsToInspect; rowIndex += 1) {
      const headerCandidates = rows[rowIndex].map(value => this.getHeaderVariants(this.toCellString(value)));
      const score = this.scoreHeaderRow(headerCandidates);

      if (score > bestScore) {
        bestScore = score;
        bestRowIndex = rowIndex;
      }
    }

    return bestScore >= 2 ? bestRowIndex : -1;
  }

  private scoreHeaderRow(headerCandidates: string[][]): number {
    const fieldsToMatch: Array<keyof BulkStudentUploadRow> = ['fullName', 'email', 'phone', 'enrollmentNumber', 'className', 'collegeName', 'collegeId'];

    return fieldsToMatch.reduce((score, field) => (
      this.findHeaderIndex(headerCandidates, field) >= 0 ? score + 1 : score
    ), 0);
  }

  private findHeaderIndex(headers: string[][], field: keyof BulkStudentUploadRow): number {
    const aliases = this.headerAliases[field].map(alias => this.normalizeHeader(alias));

    return headers.findIndex(headerVariants => headerVariants.some(header => this.matchesAlias(header, aliases)));
  }

  private matchesAlias(header: string, aliases: string[]): boolean {
    return aliases.some(alias =>
      header === alias ||
      header.includes(alias) ||
      alias.includes(header) ||
      this.containsAllAliasWords(header, alias)
    );
  }

  private containsAllAliasWords(header: string, alias: string): boolean {
    const aliasWords = alias.split(' ').filter(Boolean);
    return aliasWords.length > 0 && aliasWords.every(word => header.includes(word));
  }

  private resolveClassNameFromHeaderlessRow(cells: string[], rowNumber: number, emailIndex: number): string {
    const classIndex = emailIndex + 1;
    const className = cells[classIndex] ?? '';

    if (className.length === 0) {
      throw new Error(`Row ${rowNumber}: Class is required.`);
    }

    return className;
  }

  private looksLikeEmail(value: string): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/i.test(value.trim());
  }

  private looksLikePhone(value: string): boolean {
    const digitsOnly = value.replace(/\D/g, '');
    return digitsOnly.length >= 10;
  }

  private isEmptyRow(row: RawCell[]): boolean {
    return row.every(value => this.toCellString(value).trim().length === 0);
  }

  private toCellString(value: RawCell): string {
    return `${value ?? ''}`;
  }
}

