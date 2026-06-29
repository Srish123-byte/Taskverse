import { AfterViewInit, Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, Validators } from '@angular/forms';
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
import { SessionActivityService } from '../../../common/services/session/session-activity.service';
import { Session } from '../../../common/services/session/session.service';
import { SessionKey } from '../../../common/enums/session-key';

export type AuthMode = 'login' | 'register';

import {
  noSpecialCharsValidator,
  strictEmailValidator,
  passwordMatchValidator,
  phoneValidator,
  FULL_NAME_MAX_LENGTH,
  PHONE_MAX_LENGTH
} from '../../../common/validators/registration.validators';

@Component({
  selector: 'app-login',
  standalone: false,
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent implements OnInit, AfterViewInit, OnDestroy {
  mode: AuthMode = 'login';
  currentYear = new Date().getFullYear();

  @ViewChild('bgCanvas') private canvasRef!: ElementRef<HTMLCanvasElement>;
  private animFrame = 0;

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
    private readonly sessionActivityService: SessionActivityService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    if (this.session.isLoggedIn()) {
      void this.router.navigateByUrl(this.session.mustChangePassword
        ? `/${RouteAddress.ChangeTemporaryPassword}`
        : `/${RouteAddress.RoleDirector}`);
      return;
    }

    if (sessionStorage.getItem(SessionKey.PasswordChangeSuccess) === 'true') {
      this.successMessage = 'Password changed successfully. Please sign in with your new password.';
      sessionStorage.removeItem(SessionKey.PasswordChangeSuccess);
    }

    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]]
    });

    this.registerForm = this.fb.group({
      fullName: ['', [
        Validators.required,
        Validators.minLength(2),
        Validators.maxLength(FULL_NAME_MAX_LENGTH),
        noSpecialCharsValidator
      ]],
      email: ['', [
        Validators.required,
        strictEmailValidator
      ]],
      phone: ['', [
        Validators.required,
        phoneValidator
      ]],
      enrollmentNumber: ['', [Validators.maxLength(50)]],
      role: ['Student', Validators.required],
      collegeId: [''],
      collegeName: [''],
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

  ngAfterViewInit(): void {
    this.startCanvas();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    cancelAnimationFrame(this.animFrame);
  }

  private startCanvas(): void {
    const canvas = this.canvasRef?.nativeElement;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const resize = () => {
      canvas.width = window.innerWidth;
      canvas.height = window.innerHeight;
    };
    resize();
    window.addEventListener('resize', resize);

    const stars = Array.from({ length: 120 }, () => ({
      x: Math.random() * window.innerWidth,
      y: Math.random() * window.innerHeight,
      r: Math.random() * 1.5 + 0.3,
      a: Math.random(),
      speed: Math.random() * 0.004 + 0.001,
      phase: Math.random() * Math.PI * 2
    }));

    const draw = (t: number) => {
      ctx.clearRect(0, 0, canvas.width, canvas.height);
      stars.forEach(s => {
        s.a = 0.3 + 0.5 * Math.sin(t * s.speed + s.phase);
        ctx.beginPath();
        ctx.arc(s.x, s.y, s.r, 0, Math.PI * 2);
        ctx.fillStyle = `rgba(0, 191, 255, ${s.a})`;
        ctx.fill();
      });

      for (let i = 0; i < stars.length; i++) {
        for (let j = i + 1; j < stars.length; j++) {
          const dx = stars[i].x - stars[j].x;
          const dy = stars[i].y - stars[j].y;
          const dist = Math.sqrt(dx * dx + dy * dy);
          if (dist < 120) {
            ctx.beginPath();
            ctx.moveTo(stars[i].x, stars[i].y);
            ctx.lineTo(stars[j].x, stars[j].y);
            ctx.strokeStyle = `rgba(0, 191, 255, ${0.06 * (1 - dist / 120)})`;
            ctx.lineWidth = 0.5;
            ctx.stroke();
          }
        }
      }

      this.animFrame = requestAnimationFrame(draw);
    };
    this.animFrame = requestAnimationFrame(draw);
  }

  switchMode(mode: AuthMode): void {
    this.mode = mode;
    this.errorMessage = '';
    this.successMessage = '';
    this.loginForm.reset();
    this.registerForm.reset({ role: 'Student', collegeId: '', collegeName: '', classId: '', batchId: '' });
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
        this.session.accessTokenExpiresAt = response.expiresAt;
        this.session.lastActivityAt = new Date().toISOString();
        this.session.mustChangePassword = !!response.user.mustChangePassword;
        this.session.user = response.user;
        this.session.userEmail = response.user.email;
        this.session.userId = response.user.userId;
        this.session.role = response.user.role;
        this.sessionActivityService.registerActivity();
        if (this.session.mustChangePassword) {
          this.navigateToChangeTemporaryPassword();
          return;
        }

        this.navigateToLandingPage();
      },
      error: err => {
        const message =
          err?.error?.message ||
          (typeof err?.error === 'string' ? err.error : '') ||
          '';

        const normalizedMessage = message.toLowerCase();
        if (normalizedMessage.includes('awaiting approval') || normalizedMessage.includes('pending approval')) {
          this.redirectToApprovalStatus('', 'PENDING_APPROVAL');
          return;
        }

        if (normalizedMessage.includes('not allowed to sign in') || normalizedMessage.includes('access restricted')) {
          this.redirectToApprovalStatus('', 'REJECTED');
          return;
        }

        this.isLoading = false;
        this.errorMessage =
          message ||
          'Invalid email or password. Please try again.';
      }
    });
  }

  private redirectToApprovalStatus(role: RoleType | '', status: string): void {
    this.isLoading = false;
    this.errorMessage = '';

    void this.router.navigate([`/${RouteAddress.ApprovalStatus}`], {
      queryParams: { role, status }
    }).catch(() => {
      this.errorMessage = 'We could not open the approval status page. Please try again.';
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
    this.isLoading = false;
    void this.router.navigateByUrl(`/${RouteAddress.RoleDirector}`)
      .catch(() => {
        this.errorMessage = 'Signed in, but we could not open your dashboard.';
      });
  }

  private navigateToChangeTemporaryPassword(): void {
    this.isLoading = false;
    void this.router.navigateByUrl(`/${RouteAddress.ChangeTemporaryPassword}`)
      .catch(() => {
        this.errorMessage = 'Signed in, but we could not open the temporary password change screen.';
      });
  }

  onRegister(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    const selectedRole = this.rRole?.value;
    const classId = this.rClassId?.value?.trim?.() || '';
    const batchId = this.rBatchId?.value?.trim?.() || '';
    if (selectedRole === RoleType.Student && ((classId && !batchId) || (!classId && batchId))) {
      this.batchControl?.markAsTouched();
      this.errorMessage = 'Class and batch must either both be selected or both be left empty.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    const { confirmPassword, collegeName, ...formValue } = this.registerForm.value;
    const request: RegisterRequest = {
      ...formValue,
      enrollmentNumber: formValue.enrollmentNumber || undefined,
      collegeId: formValue.collegeId || undefined,
      classId: formValue.classId || undefined,
      batchId: formValue.batchId || undefined,
      collegeName: collegeName || undefined
    };

    this.userService.register(request).pipe(take(1)).subscribe({
      next: response => {
        const normalizedRole = this.normalizeRole(response.role) ?? this.normalizeRole(request.role);
        const normalizedStatus = this.normalizeStatus(response.status);
        const shouldRedirectToApprovalStatus =
          normalizedStatus === 'PENDING_APPROVAL' &&
          (normalizedRole === RoleType.CollegeAdmin ||
            normalizedRole === RoleType.Trainer ||
            normalizedRole === RoleType.Student);

        if (normalizedRole && shouldRedirectToApprovalStatus) {
          this.redirectToApprovalStatus(normalizedRole, normalizedStatus);
          return;
        }

        this.isLoading = false;
        const isPending = normalizedStatus === 'PENDING_APPROVAL';
        this.successMessage = isPending
          ? 'Account created! Your request is pending admin approval. You will be notified once approved.'
          : 'Account created successfully! You can now sign in.';
        this.mode = 'login';
        this.errorMessage = '';
        this.loginForm.reset();
        this.registerForm.reset({ role: 'Student', collegeId: '', collegeName: '', classId: '', batchId: '' });
        this.handleRegistrationRoleChange(RoleType.Student);
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

  get requiresClassSelection(): boolean {
    return this.rRole?.value === RoleType.Student;
  }

  get requiresBatchSelection(): boolean {
    return this.rRole?.value === RoleType.Student;
  }

  get requiresCollegeName(): boolean {
    return this.rRole?.value === RoleType.CollegeAdmin;
  }

  private handleRegistrationRoleChange(role: string | null | undefined): void {
    if (role === RoleType.CollegeAdmin) {
      this.clearInstitutionSelections();
      this.clearInstitutionValidators();
      this.applyCollegeNameValidator();
      return;
    }

    this.clearCollegeNameValue();
    this.clearCollegeNameValidator();

    if (!this.isInstitutionLinkedRole(role)) {
      this.clearInstitutionSelections();
      this.clearInstitutionValidators();
      return;
    }

    this.applyInstitutionValidators(role);
    if (this.colleges.length === 0 && !this.isCollegeOptionsLoading) {
      this.loadApprovedColleges();
    }
  }

  private handleCollegeChange(collegeId: string | null | undefined): void {
    this.resetClassesAndBatches();

    if (!this.requiresInstitutionSelection || !collegeId) {
      return;
    }

    if (!this.requiresClassSelection) {
      return;
    }

    this.loadClasses(collegeId);
  }

  private handleClassChange(classId: string | null | undefined): void {
    this.resetBatches();

    if (!this.requiresInstitutionSelection || !this.requiresBatchSelection || !classId) {
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

  private applyInstitutionValidators(role: string | null | undefined): void {
    this.collegeControl?.setValidators([Validators.required]);
    this.classControl?.setValidators([]);
    this.batchControl?.setValidators([]);

    if (role !== RoleType.Student) {
      this.registerForm.patchValue({
        classId: '',
        batchId: ''
      }, { emitEvent: false });
      this.classes = [];
      this.batches = [];
    }

    this.collegeControl?.updateValueAndValidity({ emitEvent: false });
    this.classControl?.updateValueAndValidity({ emitEvent: false });
    this.batchControl?.updateValueAndValidity({ emitEvent: false });
  }

  private applyCollegeNameValidator(): void {
    this.collegeNameControl?.setValidators([Validators.required, Validators.minLength(2)]);
    this.collegeNameControl?.updateValueAndValidity({ emitEvent: false });
  }

  private clearInstitutionValidators(): void {
    this.collegeControl?.clearValidators();
    this.classControl?.clearValidators();
    this.batchControl?.clearValidators();
    this.collegeControl?.updateValueAndValidity({ emitEvent: false });
    this.classControl?.updateValueAndValidity({ emitEvent: false });
    this.batchControl?.updateValueAndValidity({ emitEvent: false });
  }

  private clearCollegeNameValidator(): void {
    this.collegeNameControl?.clearValidators();
    this.collegeNameControl?.updateValueAndValidity({ emitEvent: false });
  }

  private clearCollegeNameValue(): void {
    this.registerForm.patchValue({
      collegeName: ''
    }, { emitEvent: false });
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
  get rEnrollmentNumber(){ return this.registerForm.get('enrollmentNumber'); }
  get rRole()            { return this.registerForm.get('role'); }
  get rCollegeId()       { return this.registerForm.get('collegeId'); }
  get rCollegeName()     { return this.registerForm.get('collegeName'); }
  get rClassId()         { return this.registerForm.get('classId'); }
  get rBatchId()         { return this.registerForm.get('batchId'); }
  get collegeControl()   { return this.registerForm.get('collegeId'); }
  get collegeNameControl(){ return this.registerForm.get('collegeName'); }
  get classControl()     { return this.registerForm.get('classId'); }
  get batchControl()     { return this.registerForm.get('batchId'); }
  get rPassword()        { return this.registerForm.get('password'); }
  get rConfirmPassword() { return this.registerForm.get('confirmPassword'); }
  get passwordMismatch() { return this.registerForm.hasError('passwordMismatch') && this.rConfirmPassword?.touched; }
}
