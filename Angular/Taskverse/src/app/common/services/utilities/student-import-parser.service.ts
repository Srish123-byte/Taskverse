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
    fullName: ['fullname', 'full_name', 'full name', 'studentname', 'student name'],
    email: ['email', 'emailaddress', 'email address'],
    phone: ['phone', 'phonenumber', 'phone number', 'mobile', 'mobile number'],
    collegeId: ['collegeid', 'college_id', 'college id'],
    classId: ['classid', 'class_id', 'class id'],
    batchId: ['batchid', 'batch_id', 'batch id']
  };

  async parse(file: File): Promise<ParsedStudentImportFile> {
    const extension = this.getExtension(file.name);
    if (!['csv', 'xls', 'xlsx'].includes(extension)) {
      throw new Error('Unsupported file format. Please upload a .csv, .xls, or .xlsx file.');
    }

    const workbook = XLSX.read(await file.arrayBuffer(), { type: 'array' });
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
      throw new Error('The selected file must include a header row and at least one student row.');
    }

    const headers = rows[0].map(value => this.normalizeHeader(this.toCellString(value)));
    const parsedRows: BulkStudentUploadRow[] = [];

    for (let rowIndex = 1; rowIndex < rows.length; rowIndex += 1) {
      const row = rows[rowIndex];
      const rowNumber = rowIndex + 1;

      if (this.isEmptyRow(row)) {
        continue;
      }

      parsedRows.push(this.mapRow(headers, row, rowNumber));
    }

    if (parsedRows.length === 0) {
      throw new Error('The selected file does not contain any populated student rows.');
    }

    return {
      fileName: file.name,
      rows: parsedRows
    };
  }

  private mapRow(headers: string[], row: RawCell[], rowNumber: number): BulkStudentUploadRow {
    const valueFor = (field: keyof BulkStudentUploadRow): string => {
      const aliases = this.headerAliases[field];
      const index = headers.findIndex(header => aliases.includes(header));
      return index >= 0 ? this.toCellString(row[index]).trim() : '';
    };

    return {
      fullName: this.requireValue(valueFor('fullName'), 'FullName', rowNumber),
      email: this.requireValue(valueFor('email'), 'Email', rowNumber),
      phone: this.requireValue(valueFor('phone'), 'Phone', rowNumber),
      collegeId: this.requireValue(valueFor('collegeId'), 'CollegeId', rowNumber),
      classId: this.requireValue(valueFor('classId'), 'ClassId', rowNumber),
      batchId: this.requireValue(valueFor('batchId'), 'BatchId', rowNumber)
    };
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
    return value.trim().toLowerCase().replace(/\s+/g, ' ');
  }

  private isEmptyRow(row: RawCell[]): boolean {
    return row.every(value => this.toCellString(value).trim().length === 0);
  }

  private toCellString(value: RawCell): string {
    return `${value ?? ''}`;
  }
}

