import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { RoleType } from '../../../common/enums/role-type.enum';
import { PendingUser } from '../../../common/models/super-admin.model';
import { CollegeAdminService } from '../../../common/services/api/college-admin.service';

@Component({
  selector: 'app-college-admin-user-management',
  standalone: false,
  templateUrl: './user-management.component.html',
  styleUrl: './user-management.component.scss'
})
export class UserManagementComponent implements OnInit, OnDestroy {
  pendingUsers: PendingUser[] = [];
  selectedRole = 'all';
  searchTerm = '';
  isLoading = false;
  activeUserId: string | null = null;
  errorMessage = '';
  actionMessage = '';
  private readonly subscriptions = new Subscription();

  constructor(
    private readonly collegeAdminService: CollegeAdminService,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.subscriptions.add(
      this.collegeAdminService.pendingUsers$.subscribe(users => {
        this.pendingUsers = users.filter(user => user.role === RoleType.Student || user.role === RoleType.Trainer);
        this.changeDetectorRef.detectChanges();
      })
    );

    this.loadPendingUsers();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  trackByUserId(_: number, user: PendingUser): string {
    return user.userId;
  }

  get pendingApprovalCount(): number {
    return this.pendingUsers.length;
  }

  get filteredUsers(): PendingUser[] {
    return this.pendingUsers.filter(user => {
      const matchesRole = this.selectedRole === 'all' || user.role === this.selectedRole;
      const normalizedSearch = this.searchTerm.trim().toLowerCase();
      const matchesSearch =
        normalizedSearch.length === 0 ||
        user.fullName.toLowerCase().includes(normalizedSearch) ||
        user.email.toLowerCase().includes(normalizedSearch);

      return matchesRole && matchesSearch;
    });
  }

  get availableRoles(): string[] {
    return [...new Set(this.pendingUsers.map(user => user.role))].sort((left, right) => left.localeCompare(right));
  }

  getRoleLabel(role: string): string {
    return role.replace(/([a-z])([A-Z])/g, '$1 $2').toUpperCase();
  }

  getRoleClass(role: string): string {
    return role
      .replace(/([a-z])([A-Z])/g, '$1-$2')
      .replace(/\s+/g, '-')
      .toLowerCase();
  }

  resetFilters(): void {
    this.selectedRole = 'all';
    this.searchTerm = '';
  }

  isActingOn(userId: string): boolean {
    return this.activeUserId === userId;
  }

  approveUser(user: PendingUser): void {
    if (this.activeUserId) {
      return;
    }

    this.activeUserId = user.userId;
    this.errorMessage = '';
    this.actionMessage = '';

    this.collegeAdminService.approveUser(user.userId).subscribe({
      next: () => {
        this.actionMessage = `${user.fullName} has been approved.`;
        this.activeUserId = null;
        this.changeDetectorRef.detectChanges();
      },
      error: err => {
        this.errorMessage =
          err?.error?.detail ||
          err?.error?.message ||
          'Unable to approve this user right now.';
        this.activeUserId = null;
        this.changeDetectorRef.detectChanges();
      }
    });
  }

  rejectUser(user: PendingUser): void {
    if (this.activeUserId) {
      return;
    }

    this.activeUserId = user.userId;
    this.errorMessage = '';
    this.actionMessage = '';

    this.collegeAdminService.rejectUser(user.userId).subscribe({
      next: () => {
        this.actionMessage = `${user.fullName} has been rejected.`;
        this.activeUserId = null;
        this.changeDetectorRef.detectChanges();
      },
      error: err => {
        this.errorMessage =
          err?.error?.detail ||
          err?.error?.message ||
          'Unable to reject this user right now.';
        this.activeUserId = null;
        this.changeDetectorRef.detectChanges();
      }
    });
  }

  private loadPendingUsers(): void {
    if (this.isLoading) {
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.actionMessage = '';

    this.collegeAdminService.getPendingUsers().subscribe({
      next: () => {
        this.isLoading = false;
        this.changeDetectorRef.detectChanges();
      },
      error: () => {
        this.errorMessage = 'Unable to load pending users right now.';
        this.isLoading = false;
        this.changeDetectorRef.detectChanges();
      }
    });
  }
}
