import { Component, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AccountService, LoginRequest, LoginResponse } from '../../../common/services/api/account.service';
import { UserService, RegisterRequest } from '../../../common/services/api/user.service';
import { Session } from '../../../common/services/session/session.service';
import { RouteAddress } from '../../../common/constants/routes.constants';
import { RoleType } from '../../../common/enums/role-type.enum';
import { take } from 'rxjs/operators';

export type AuthMode = 'login' | 'register';

const passwordMatchValidator: ValidatorFn = (group: AbstractControl): ValidationErrors | null => {
  const pw  = group.get('password')?.value;
  const cpw = group.get('confirmPassword')?.value;
  return pw && cpw && pw !== cpw ? { passwordMismatch: true } : null;
};

@Component({
  selector: 'app-login',
  standalone: false,
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent implements OnInit {
  mode: AuthMode = 'login';

  loginForm!: FormGroup;
  registerForm!: FormGroup;

  isLoading     = false;
  errorMessage  = '';
  successMessage = '';

  readonly roles = [
    { value: 'Student',      label: 'Student'       },
    { value: 'Trainer',      label: 'Trainer'       },
    { value: 'CollegeAdmin', label: 'College Admin' },
    { value: 'SuperAdmin',   label: 'Super Admin'   }
  ];

  constructor(
    private readonly fb: FormBuilder,
    private readonly accountService: AccountService,
    private readonly userService: UserService,
    private readonly session: Session,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    if (this.session.isLoggedIn()) {
      void this.router.navigateByUrl(`/${RouteAddress.RoleDirector}`);
      return;
    }

    this.loginForm = this.fb.group({
      email:    ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]]
    });

    this.registerForm = this.fb.group({
      fullName:        ['', [Validators.required, Validators.minLength(2)]],
      email:           ['', [Validators.required, Validators.email]],
      phone:           [''],   // optional — no pattern restriction
      role:            ['Student', Validators.required],
      password:        ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', Validators.required]
    }, { validators: passwordMatchValidator });
  }

  switchMode(mode: AuthMode): void {
    this.mode           = mode;
    this.errorMessage   = '';
    this.successMessage = '';
    this.loginForm.reset();
    this.registerForm.reset({ role: 'Student' });
  }

  // ─── Login ────────────────────────────────────────────────────────────────
  onLogin(): void {
    if (this.loginForm.invalid) { this.loginForm.markAllAsTouched(); return; }

    this.isLoading    = true;
    this.errorMessage = '';

    const request: LoginRequest = this.loginForm.value;

    this.accountService.login(request).pipe(take(1)).subscribe({
      next: (response) => {
        const normalizedRole = this.normalizeRole(response.user?.role);
        if (!normalizedRole || !response.user || !response.token) {
          this.isLoading = false;
          this.errorMessage = 'Login succeeded, but the server returned an unexpected response.';
          return;
        }

        response.user.role = normalizedRole;
        const normalizedStatus = this.normalizeStatus(response.user.status);

        if (normalizedStatus !== 'APPROVED') {
          this.redirectToApprovalStatus(response.user.role, normalizedStatus);
          return;
        }

        response.user.status = normalizedStatus;
        this.session.jwtToken = response.token;
        this.session.refreshToken = response.refreshToken;
        this.session.user = response.user;
        this.session.userEmail = response.user.email;
        this.session.userId = response.user.userId;
        this.session.role = response.user.role;
        this.navigateToLandingPage();
      },
      error: (err) => {
        this.isLoading    = false;
        this.errorMessage =
          err?.error?.message ||
          (typeof err?.error === 'string' ? err.error : '') ||
          'Invalid email or password. Please try again.';
      }
    });
  }

  // ─── Helper Methods ───────────────────────────────────────────────────────
  private redirectToApprovalStatus(role: RoleType, status: string): void {
    void this.router.navigate([`/${RouteAddress.ApprovalStatus}`], {
      queryParams: {
        role,
        status
      }
    }).finally(() => {
      this.isLoading = false;
    });
  }

  // ─── Register ─────────────────────────────────────────────────────────────
  private normalizeRole(role: string | RoleType | undefined | null): RoleType | null {
    switch ((role ?? '').toString().trim().toLowerCase()) {
      case 'superadmin':
      case 'super-admin':
      case 'super_admin':
        return RoleType.SuperAdmin;
      case 'collegeadmin':
      case 'college-admin':
      case 'college_admin':
      case 'college admin':
        return RoleType.CollegeAdmin;
      case 'trainer':
        return RoleType.Trainer;
      case 'student':
        return RoleType.Student;
      default:
        return null;
    }
  }

  private normalizeStatus(status: string | null | undefined): string {
    return (status ?? '').toString().trim().toUpperCase();
  }

  private navigateToLandingPage(): void {
    void this.router.navigateByUrl(`/${RouteAddress.RoleDirector}`)
      .catch(() => {
        this.errorMessage = 'Signed in, but we could not open your dashboard.';
      })
      .finally(() => {
        this.isLoading = false;
      });
  }

  onRegister(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.isLoading      = true;
    this.errorMessage   = '';
    this.successMessage = '';

    const { confirmPassword, ...formValue } = this.registerForm.value;
    const request: RegisterRequest = formValue;

    this.userService.register(request).pipe(take(1)).subscribe({
      next: (response) => {
        this.isLoading = false;
        const isPending = response.status === 'PENDING_APPROVAL';
        this.successMessage = isPending
          ? 'Account created! Your request is pending admin approval. You will be notified once approved.'
          : 'Account created successfully! You can now sign in.';
        this.switchMode('login');
      },
      error: (err) => {
        this.isLoading    = false;
        this.errorMessage = err?.error?.message?.includes('already exists')
          ? 'An account with this email already exists. Please sign in.'
          : 'Registration failed. Please check your details and try again.';
      }
    });
  }

  // ─── Getters ──────────────────────────────────────────────────────────────
  get email()           { return this.loginForm.get('email');              }
  get password()        { return this.loginForm.get('password');           }
  get rFullName()       { return this.registerForm.get('fullName');        }
  get rEmail()          { return this.registerForm.get('email');           }
  get rPhone()          { return this.registerForm.get('phone');           }
  get rRole()           { return this.registerForm.get('role');            }
  get rPassword()       { return this.registerForm.get('password');        }
  get rConfirmPassword(){ return this.registerForm.get('confirmPassword'); }
  get passwordMismatch(){ return this.registerForm.hasError('passwordMismatch') && this.rConfirmPassword?.touched; }
}
