import { Component } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';
import { Router } from '@angular/router';
import { RouteAddress } from '../../constants/routes.constants';
import { AccountService } from '../../services/api/account.service';
import { Session } from '../../services/session/session.service';

type ChangePasswordField = 'currentPassword' | 'newPassword' | 'confirmPassword';

@Component({
  selector: 'app-change-password-dialog',
  standalone: false,
  templateUrl: './change-password-dialog.component.html',
  styleUrl: './change-password-dialog.component.scss'
})
export class ChangePasswordDialogComponent {
  isSubmitting = false;
  errorMessage = '';
  private readonly visibleFields = new Set<ChangePasswordField>();

  readonly form;

  constructor(
    private readonly dialogRef: MatDialogRef<ChangePasswordDialogComponent, boolean>,
    private readonly formBuilder: FormBuilder,
    private readonly accountService: AccountService,
    private readonly session: Session,
    private readonly router: Router
  ) {
    this.form = this.formBuilder.group({
      currentPassword: ['', [Validators.required, Validators.minLength(8)]],
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', [Validators.required, Validators.minLength(8)]]
    });
  }

  isPasswordVisible(field: ChangePasswordField): boolean {
    return this.visibleFields.has(field);
  }

  togglePasswordVisibility(field: ChangePasswordField): void {
    if (this.visibleFields.has(field)) {
      this.visibleFields.delete(field);
    } else {
      this.visibleFields.add(field);
    }
  }

  cancel(): void {
    if (this.isSubmitting) {
      return;
    }

    this.dialogRef.close(false);
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const currentPassword = this.form.controls.currentPassword.value ?? '';
    const newPassword = this.form.controls.newPassword.value ?? '';
    const confirmPassword = this.form.controls.confirmPassword.value ?? '';

    if (newPassword !== confirmPassword) {
      this.errorMessage = 'The new password and confirmation do not match.';
      return;
    }

    if (currentPassword === newPassword) {
      this.errorMessage = 'Please choose a password different from your current one.';
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';

    this.accountService.changePassword({
      currentPassword,
      newPassword,
      isTemporaryPasswordChange: false
    }).subscribe({
      next: () => {
        this.dialogRef.close(true);
        this.session.clear();
        void this.router.navigateByUrl(`/${RouteAddress.Login}`);
      },
      error: err => {
        this.isSubmitting = false;
        this.errorMessage = err?.error?.message || 'Unable to change your password right now.';
      }
    });
  }
}
