import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

// ─── Full Name ───────────────────────────────────────────────────────────────

export const FULL_NAME_MAX_LENGTH = 50;

/**
 * Rejects any character that is not a letter, digit, space, hyphen, or apostrophe.
 * Use alongside Validators.required, Validators.minLength, and Validators.maxLength.
 */
export const noSpecialCharsValidator: ValidatorFn =
  (control: AbstractControl): ValidationErrors | null => {
    const value: string = control.value ?? '';
    return /[^a-zA-Z0-9 '\-]/.test(value) ? { specialChars: true } : null;
  };

// ─── Email ────────────────────────────────────────────────────────────────────

/**
 * Strict email validator: enforces the presence of @, a valid domain structure,
 * and requires the domain to end with .com (case-insensitive).
 * Pair with Validators.required — returns null on empty so required handles that case.
 */
export const strictEmailValidator: ValidatorFn =
  (control: AbstractControl): ValidationErrors | null => {
    const value: string = (control.value ?? '').trim();
    if (!value) return null;
    const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    const hasDotCom    = /\.com$/i.test(value);
    return emailPattern.test(value) && hasDotCom ? null : { strictEmail: true };
  };

// ─── Phone ────────────────────────────────────────────────────────────────────

export const PHONE_MAX_LENGTH = 11;

/**
 * Validates that the phone number contains digits only and is at most 11 digits long.
 * Pair with Validators.required for mandatory fields.
 */
export const phoneValidator: ValidatorFn =
  (control: AbstractControl): ValidationErrors | null => {
    const value: string = (control.value ?? '').trim();
    if (!value) return null;
    return /^\d{1,11}$/.test(value) ? null : { phone: true };
  };

// ─── Password match (cross-field) ────────────────────────────────────────────

/**
 * Group-level validator: checks that `password` and `confirmPassword` fields match.
 * Apply to the FormGroup, not a single control.
 */
export const passwordMatchValidator: ValidatorFn =
  (group: AbstractControl): ValidationErrors | null => {
    const pw  = group.get('password')?.value;
    const cpw = group.get('confirmPassword')?.value;
    return pw && cpw && pw !== cpw ? { passwordMismatch: true } : null;
  };
