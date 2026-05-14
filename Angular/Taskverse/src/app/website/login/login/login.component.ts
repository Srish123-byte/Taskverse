import { Component, OnDestroy, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { take } from 'rxjs/operators';
import { AccountService, LoginRequest } from '../../../common/services/api/account.service';
import {
  RegistrationBatchOption,
  RegistrationClassOption,
  RegistrationCollegeOption,
  RegisterRequest,
  UserService
} from '../../../common/services/api/user.service';
import { RouteAddress } from '../../../common/constants/routes.constants';
import { RoleType } from '../../../common/enums/role-type.enum';
import { Session } from '../../../common/services/session/session.service';

export type AuthMode = 'login' | 'register';

const passwordMatchValidator: ValidatorFn = (group: AbstractControl): ValidationErrors | null => {
  const pw = group.get('password')?.value;
  const cpw = group.get('confirmPassword')?.value;
  return pw && cpw && pw !== cpw ? { passwordMismatch: true } : null;
};

@Component({
  selector: 'app-login',
  standalone: false,
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent implements OnInit, OnDestroy {
  mode: AuthMode = 'login';

  loginForm!: FormGroup;
  registerForm!: FormGroup;

  isLoading = false;
  errorMessage = '';
  successMessage = '';
  colleges: RegistrationCollegeOption[] = [];
  classes: RegistrationClassOption[] = [];
  batches: RegistrationBatchOption[] = [];
  isCollegeOptionsLoading = false;
  isClassOptionsLoading = false;
  isBatchOptionsLoading = false;
  private readonly subscriptions = new Subscription();

  readonly roles = [
    { value: 'Student', label: 'Student' },
    { value: 'Trainer', label: 'Trainer' },
    { value: 'CollegeAdmin', label: 'College Admin' },
    { value: 'SuperAdmin', label: 'Super Admin' }
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
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]]
    });

    this.registerForm = this.fb.group({
      fullName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      phone: [''],
      role: ['Student', Validators.required],
      collegeId: [''],
      classId: [''],
      batchId: [''],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', Validators.required]
    }, { validators: passwordMatchValidator });

    const roleChanges = this.rRole?.valueChanges;
    if (roleChanges) {
      this.subscriptions.add(roleChanges.subscribe(role => this.handleRegistrationRoleChange(role)));
    }

    const collegeChanges = this.collegeControl?.valueChanges;
    if (collegeChanges) {
      this.subscriptions.add(collegeChanges.subscribe(collegeId => this.handleCollegeChange(collegeId)));
    }

    const classChanges = this.classControl?.valueChanges;
    if (classChanges) {
      this.subscriptions.add(classChanges.subscribe(classId => this.handleClassChange(classId)));
    }

    this.handleRegistrationRoleChange(this.rRole?.value);
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  switchMode(mode: AuthMode): void {
    this.mode = mode;
    this.errorMessage = '';
    this.successMessage = '';
    this.loginForm.reset();
    this.registerForm.reset({ role: 'Student', collegeId: '', classId: '', batchId: '' });
    this.handleRegistrationRoleChange(RoleType.Student);
  }

  onLogin(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    const request: LoginRequest = this.loginForm.value;

    this.accountService.login(request).pipe(take(1)).subscribe({
      next: response => {
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
      error: err => {
        this.isLoading = false;
        this.errorMessage =
          err?.error?.message ||
          (typeof err?.error === 'string' ? err.error : '') ||
          'Invalid email or password. Please try again.';
      }
    });
  }

  private redirectToApprovalStatus(role: RoleType, status: string): void {
    void this.router.navigate([`/${RouteAddress.ApprovalStatus}`], {
      queryParams: { role, status }
    }).finally(() => {
      this.isLoading = false;
    });
  }

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

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    const { confirmPassword, ...formValue } = this.registerForm.value;
    const request: RegisterRequest = formValue;

    this.userService.register(request).pipe(take(1)).subscribe({
      next: response => {
        this.isLoading = false;
        const isPending = response.status === 'PENDING_APPROVAL';
        this.successMessage = isPending
          ? 'Account created! Your request is pending admin approval. You will be notified once approved.'
          : 'Account created successfully! You can now sign in.';
        this.switchMode('login');
      },
      error: err => {
        this.isLoading = false;
        this.errorMessage = err?.error?.message?.includes('already exists')
          ? 'An account with this email already exists. Please sign in.'
          : 'Registration failed. Please check your details and try again.';
      }
    });
  }

  get requiresInstitutionSelection(): boolean {
    return this.isInstitutionLinkedRole(this.rRole?.value);
  }

  private handleRegistrationRoleChange(role: string | null | undefined): void {
    if (!this.isInstitutionLinkedRole(role)) {
      this.clearInstitutionSelections();
      this.clearInstitutionValidators();
      return;
    }

    this.applyInstitutionValidators();
    if (this.colleges.length === 0 && !this.isCollegeOptionsLoading) {
      this.loadApprovedColleges();
    }
  }

  private handleCollegeChange(collegeId: string | null | undefined): void {
    this.resetClassesAndBatches();

    if (!this.requiresInstitutionSelection || !collegeId) {
      return;
    }

    this.loadClasses(collegeId);
  }

  private handleClassChange(classId: string | null | undefined): void {
    this.resetBatches();

    if (!this.requiresInstitutionSelection || !classId) {
      return;
    }

    this.loadBatches(classId);
  }

  private loadApprovedColleges(): void {
    this.isCollegeOptionsLoading = true;

    this.userService.getApprovedRegistrationColleges().pipe(take(1)).subscribe({
      next: colleges => {
        this.colleges = colleges;
        this.isCollegeOptionsLoading = false;
      },
      error: () => {
        this.colleges = [];
        this.isCollegeOptionsLoading = false;
      }
    });
  }

  private loadClasses(collegeId: string): void {
    this.isClassOptionsLoading = true;

    this.userService.getRegistrationClasses(collegeId).pipe(take(1)).subscribe({
      next: classes => {
        this.classes = classes;
        this.isClassOptionsLoading = false;
      },
      error: () => {
        this.classes = [];
        this.isClassOptionsLoading = false;
      }
    });
  }

  private loadBatches(classId: string): void {
    this.isBatchOptionsLoading = true;

    this.userService.getRegistrationBatches(classId).pipe(take(1)).subscribe({
      next: batches => {
        this.batches = batches;
        this.isBatchOptionsLoading = false;
      },
      error: () => {
        this.batches = [];
        this.isBatchOptionsLoading = false;
      }
    });
  }

  private applyInstitutionValidators(): void {
    this.collegeControl?.setValidators([Validators.required]);
    this.classControl?.setValidators([Validators.required]);
    this.batchControl?.setValidators([Validators.required]);
    this.collegeControl?.updateValueAndValidity({ emitEvent: false });
    this.classControl?.updateValueAndValidity({ emitEvent: false });
    this.batchControl?.updateValueAndValidity({ emitEvent: false });
  }

  private clearInstitutionValidators(): void {
    this.collegeControl?.clearValidators();
    this.classControl?.clearValidators();
    this.batchControl?.clearValidators();
    this.collegeControl?.updateValueAndValidity({ emitEvent: false });
    this.classControl?.updateValueAndValidity({ emitEvent: false });
    this.batchControl?.updateValueAndValidity({ emitEvent: false });
  }

  private clearInstitutionSelections(): void {
    this.registerForm.patchValue({
      collegeId: '',
      classId: '',
      batchId: ''
    }, { emitEvent: false });

    this.colleges = [];
    this.classes = [];
    this.batches = [];
    this.isCollegeOptionsLoading = false;
    this.isClassOptionsLoading = false;
    this.isBatchOptionsLoading = false;
  }

  private resetClassesAndBatches(): void {
    this.registerForm.patchValue({
      classId: '',
      batchId: ''
    }, { emitEvent: false });

    this.classes = [];
    this.batches = [];
    this.isClassOptionsLoading = false;
    this.isBatchOptionsLoading = false;
  }

  private resetBatches(): void {
    this.registerForm.patchValue({
      batchId: ''
    }, { emitEvent: false });

    this.batches = [];
    this.isBatchOptionsLoading = false;
  }

  private isInstitutionLinkedRole(role: string | null | undefined): boolean {
    return role === RoleType.Student || role === RoleType.Trainer;
  }

  get email()            { return this.loginForm.get('email'); }
  get password()         { return this.loginForm.get('password'); }
  get rFullName()        { return this.registerForm.get('fullName'); }
  get rEmail()           { return this.registerForm.get('email'); }
  get rPhone()           { return this.registerForm.get('phone'); }
  get rRole()            { return this.registerForm.get('role'); }
  get rCollegeId()       { return this.registerForm.get('collegeId'); }
  get rClassId()         { return this.registerForm.get('classId'); }
  get rBatchId()         { return this.registerForm.get('batchId'); }
  get collegeControl()   { return this.registerForm.get('collegeId'); }
  get classControl()     { return this.registerForm.get('classId'); }
  get batchControl()     { return this.registerForm.get('batchId'); }
  get rPassword()        { return this.registerForm.get('password'); }
  get rConfirmPassword() { return this.registerForm.get('confirmPassword'); }
  get passwordMismatch() { return this.registerForm.hasError('passwordMismatch') && this.rConfirmPassword?.touched; }
}
