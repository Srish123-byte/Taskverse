import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AccountService, LoginRequest } from '../../../common/services/api/account.service';
import { Session } from '../../../common/services/session/session.service';
import { RouteAddress } from '../../../common/constants/routes.constants';
import { take } from 'rxjs/operators';

@Component({
  selector: 'app-login',
  standalone: false,
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent implements OnInit {
  loginForm!: FormGroup;
  isLoading = false;
  errorMessage = '';

  constructor(
    private readonly fb: FormBuilder,
    private readonly accountService: AccountService,
    private readonly session: Session,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    // Redirect already-authenticated users straight to the director
    if (this.session.isLoggedIn()) {
      this.router.navigate([RouteAddress.RoleDirector]);
      return;
    }

    this.loginForm = this.fb.group({
      email:    ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]]
    });
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isLoading    = true;
    this.errorMessage = '';

    const request: LoginRequest = this.loginForm.value;

    this.accountService
      .login(request)
      .pipe(take(1))
      .subscribe({
        next: (response) => {
          this.session.jwtToken  = response.token;
          this.session.user      = response.user;
          this.session.userEmail = response.user.email;
          this.session.userId    = response.user.userId;
          this.session.role      = response.user.role;
          this.router.navigate([RouteAddress.RoleDirector]);
        },
        error: () => {
          this.isLoading     = false;
          this.errorMessage  = 'Invalid email or password. Please try again.';
        }
      });
  }

  get email()    { return this.loginForm.get('email');    }
  get password() { return this.loginForm.get('password'); }
}
