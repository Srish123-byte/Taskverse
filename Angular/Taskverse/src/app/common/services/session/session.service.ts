import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { SessionKey } from '../../enums/session-key';
import { RoleType } from '../../enums/role-type.enum';
import { User } from '../../models/user.model';

@Injectable({ providedIn: 'root' })
export class Session {
  private readonly storage = sessionStorage;

  private readonly _user$ = new BehaviorSubject<User | null>(null);
  public readonly user$: Observable<User | null> = this._user$.asObservable();

  // JWT token
  get jwtToken(): string {
    return this.storage.getItem(SessionKey.JwtToken) as string;
  }
  set jwtToken(value: string) {
    this.storage.setItem(SessionKey.JwtToken, value);
  }

  // Refresh token
  get refreshToken(): string {
    return this.storage.getItem(SessionKey.RefreshToken) as string;
  }
  set refreshToken(value: string) {
    this.storage.setItem(SessionKey.RefreshToken, value);
  }

  get accessTokenExpiresAt(): string {
    return this.storage.getItem(SessionKey.AccessTokenExpiresAt) as string;
  }
  set accessTokenExpiresAt(value: string) {
    this.storage.setItem(SessionKey.AccessTokenExpiresAt, value);
  }

  get lastActivityAt(): string {
    return this.storage.getItem(SessionKey.LastActivityAt) as string;
  }
  set lastActivityAt(value: string) {
    this.storage.setItem(SessionKey.LastActivityAt, value);
  }

  // User email
  get userEmail(): string {
    return this.storage.getItem(SessionKey.UserEmail) as string;
  }
  set userEmail(value: string) {
    this.storage.setItem(SessionKey.UserEmail, value);
  }

  // User ID
  get userId(): string {
    return this.storage.getItem(SessionKey.UserId) as string;
  }
  set userId(value: string) {
    this.storage.setItem(SessionKey.UserId, value);
  }

  // Role
  get role(): RoleType | null {
    return this.storage.getItem(SessionKey.Role) as RoleType | null;
  }
  set role(value: RoleType) {
    this.storage.setItem(SessionKey.Role, value);
    if (!this.isCollegeScopedRole(value)) {
      this.storage.removeItem(SessionKey.CollegeId);
    }
  }

  get collegeId(): string {
    return this.storage.getItem(SessionKey.CollegeId) as string;
  }
  set collegeId(value: string) {
    this.storage.setItem(SessionKey.CollegeId, value);
  }

  get mustChangePassword(): boolean {
    return this.storage.getItem(SessionKey.MustChangePassword) === 'true';
  }
  set mustChangePassword(value: boolean) {
    this.storage.setItem(SessionKey.MustChangePassword, String(value));
  }

  // In-memory user profile (reactive)
  get user(): User | null {
    return this._user$.value;
  }
  set user(value: User | null) {
    this._user$.next(value);
    this.syncCollegeId(value);
  }

  isLoggedIn(): boolean {
    return !!this.jwtToken && !!this.role;
  }

  clear(): void {
    this._user$.next(null);
    this.storage.removeItem(SessionKey.JwtToken);
    this.storage.removeItem(SessionKey.RefreshToken);
    this.storage.removeItem(SessionKey.AccessTokenExpiresAt);
    this.storage.removeItem(SessionKey.LastActivityAt);
    this.storage.removeItem(SessionKey.UserEmail);
    this.storage.removeItem(SessionKey.UserId);
    this.storage.removeItem(SessionKey.Role);
    this.storage.removeItem(SessionKey.CollegeId);
    this.storage.removeItem(SessionKey.MustChangePassword);
  }

  private syncCollegeId(user: User | null): void {
    const role = user?.role ?? this.role;
    const collegeId = user?.collegeId?.trim();

    if (this.isCollegeScopedRole(role) && collegeId) {
      this.collegeId = collegeId;
      return;
    }

    if (!this.isCollegeScopedRole(role)) {
      this.storage.removeItem(SessionKey.CollegeId);
    }
  }

  private isCollegeScopedRole(role: RoleType | null | undefined): boolean {
    return role === RoleType.CollegeAdmin || role === RoleType.Trainer || role === RoleType.Student;
  }
}
