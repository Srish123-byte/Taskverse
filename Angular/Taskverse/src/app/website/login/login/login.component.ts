import { Component, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AccountService, LoginRequest } from '../../../common/services/api/account.service';
import { UserService, RegisterRequest } from '../../../common/services/api/user.service';
import { Session } from '../../../common/services/session/session.service';
import { RouteAddress } from '../../../common/constants/routes.constants';
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
      this.router.navigate([RouteAddress.RoleDirector]);
      return;
    }

    this.loginForm = this.fb.group({
      email:    ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]]
    });

    this.registerForm = this.fb.group({
      fullName:        ['', [Validators.required, Validators.minLength(2)]],
      email:           ['', [Validators.required, Validators.email]],
      phone:           ['', [Validators.pattern(/^\+?[0-9\s\-]{7,15}$/)]],
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
        this.session.jwtToken  = response.token;
        this.session.user      = response.user;
        this.session.userEmail = response.user.email;
        this.session.userId    = response.user.userId;
        this.session.role      = response.user.role;
        this.router.navigate([RouteAddress.RoleDirector]);
      },
      error: () => {
        this.isLoading    = false;
        this.errorMessage = 'Invalid email or password. Please try again.';
      }
    });
  }

  // ─── Register ─────────────────────────────────────────────────────────────
  onRegister(): void {
    if (this.registerForm.invalid) { this.registerForm.markAllAsTouched(); return; }

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
