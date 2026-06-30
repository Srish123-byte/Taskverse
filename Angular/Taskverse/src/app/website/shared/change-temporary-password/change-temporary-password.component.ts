import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AccountService } from '../../../common/services/api/account.service';
import { RouteAddress } from '../../../common/constants/routes.constants';
import { SessionKey } from '../../../common/enums/session-key';
import { Session } from '../../../common/services/session/session.service';

@Component({
  selector: 'app-change-temporary-password',
  standalone: false,
  templateUrl: './change-temporary-password.component.html',
  styleUrl: './change-temporary-password.component.scss'
})
export class ChangeTemporaryPasswordComponent implements OnInit {
  isSubmitting = false;
  errorMessage = '';

  readonly form;

  constructor(
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

  ngOnInit(): void {
    if (!this.session.isLoggedIn()) {
      void this.router.navigateByUrl(`/${RouteAddress.Login}`);
      return;
    }

    if (!this.session.mustChangePassword) {
      void this.router.navigateByUrl(`/${RouteAddress.RoleDirector}`);
    }
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
      this.errorMessage = 'Please choose a different password from the temporary one.';
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';

    this.accountService.changePassword({
      currentPassword,
      newPassword,
      isTemporaryPasswordChange: true
    }).subscribe({
      next: () => {
        sessionStorage.setItem(SessionKey.PasswordChangeSuccess, 'true');
        this.session.clear();
        void this.router.navigateByUrl(`/${RouteAddress.Login}`);
      },
      error: err => {
        this.isSubmitting = false;
        this.errorMessage = err?.error?.message || 'Unable to change the temporary password right now.';
      }
    });
  }
}
