import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export const CLASS_OR_BATCH_NAME_MAX_LENGTH = 10;
export const CLASS_OR_BATCH_NAME_PATTERN = /^[A-Za-z0-9/-]+$/;
export const CLASS_OR_BATCH_NAME_HINT =
  `Maximum ${CLASS_OR_BATCH_NAME_MAX_LENGTH} characters. Only letters, numbers, "/" and "-" are allowed.`;

export function classOrBatchNameValidator(maxLength = CLASS_OR_BATCH_NAME_MAX_LENGTH): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = `${control.value ?? ''}`;

    if (!value) {
      return null;
    }

    if (value.length > maxLength) {
      return {
        restrictedNameLength: {
          requiredLength: maxLength,
          actualLength: value.length
        }
      };
    }

    if (!CLASS_OR_BATCH_NAME_PATTERN.test(value)) {
      return {
        restrictedNamePattern: {
          allowedCharacters: 'letters, numbers, "/" and "-"'
        }
      };
    }

    return null;
  };
}
