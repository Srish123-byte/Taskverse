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
  }

  // In-memory user profile (reactive)
  get user(): User | null {
    return this._user$.value;
  }
  set user(value: User | null) {
    this._user$.next(value);
  }

  isLoggedIn(): boolean {
    return !!this.jwtToken && !!this.role;
  }

  clear(): void {
    this._user$.next(null);
    this.storage.removeItem(SessionKey.JwtToken);
    this.storage.removeItem(SessionKey.UserEmail);
    this.storage.removeItem(SessionKey.UserId);
    this.storage.removeItem(SessionKey.Role);
  }
}
