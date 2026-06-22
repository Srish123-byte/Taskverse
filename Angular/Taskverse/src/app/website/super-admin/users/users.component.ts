import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { NavigationEnd, Router } from '@angular/router';
import { Subject, Subscription, filter, takeUntil } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { PendingUser, UserSearchRequest } from '../../../common/models/super-admin.model';
import { SuperAdminService } from '../../../common/services/api/super-admin.service';

@Component({
  selector: 'app-super-admin-users',
  standalone: false,
  templateUrl: './users.component.html',
  styleUrl: './users.component.scss'
})
export class UsersComponent implements OnInit, OnDestroy {
  private static readonly actionSnackBarConfig = {
    duration: 3500,
    horizontalPosition: 'center' as const,
    verticalPosition: 'top' as const,
    panelClass: ['question-editor-success-snackbar']
  };

  // Data
  users: PendingUser[] = [];
  availableRoles: string[] = [];

  // Filters
  selectedStatus = 'all';
  selectedRole = 'all';
  searchTerm = '';

  // Pagination
  currentPage = 1;
  readonly pageSize = 10;
  totalCount = 0;

  // State
  isLoading = false;
  activeUserId: string | null = null;
  errorMessage = '';

  private readonly searchSubject = new Subject<void>();
  private routeSubscription?: Subscription;
  private destroy$ = new Subject<void>();

  readonly statusOptions = [
    { value: 'all',              label: 'All Statuses' },
    { value: 'PENDING_APPROVAL', label: 'Pending'      },
    { value: 'APPROVED',         label: 'Approved'     },
    { value: 'REJECTED',         label: 'Rejected'     }
  ];

  constructor(
    private readonly superAdminService: SuperAdminService,
    private readonly router: Router,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    // Debounce text-search so we don't fire on every keystroke
    this.searchSubject.pipe(
      debounceTime(350),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(() => {
      this.currentPage = 1;
      this.loadUsers();
    });

    this.routeSubscription = this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe(e => {
        if (e.urlAfterRedirects.endsWith('/super-admin/users') &&
            (this.users.length === 0 || this.errorMessage)) {
          this.loadUsers();
        }
      });

    if (this.router.url.endsWith('/super-admin/users')) {
      this.loadUsers();
    }
  }

  ngOnDestroy(): void {
    this.routeSubscription?.unsubscribe();
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── Pagination ──────────────────────────────────────────────────────
  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  get pageStart(): number {
    return this.totalCount === 0 ? 0 : (this.currentPage - 1) * this.pageSize + 1;
  }

  get pageEnd(): number {
    return Math.min(this.currentPage * this.pageSize, this.totalCount);
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages || page === this.currentPage) return;
    this.currentPage = page;
    this.loadUsers();
  }

  prevPage(): void { this.goToPage(this.currentPage - 1); }
  nextPage(): void { this.goToPage(this.currentPage + 1); }

  get pageNumbers(): number[] {
    const pages: number[] = [];
    const start = Math.max(1, this.currentPage - 2);
    const end   = Math.min(this.totalPages, this.currentPage + 2);
    for (let i = start; i <= end; i++) pages.push(i);
    return pages;
  }

  // ── Filters ──────────────────────────────────────────────────────────
  onStatusChange(): void  { this.currentPage = 1; this.loadUsers(); }
  onRoleChange(): void    { this.currentPage = 1; this.loadUsers(); }
  onSearchChange(): void  { this.searchSubject.next(); }

  resetFilters(): void {
    this.selectedStatus = 'all';
    this.selectedRole   = 'all';
    this.searchTerm     = '';
    this.currentPage    = 1;
    this.loadUsers();
  }

  // ── Display helpers ──────────────────────────────────────────────────
  trackByUserId(_: number, user: PendingUser): string { return user.userId; }

  getRoleLabel(role: string): string {
    return role.replace(/([a-z])([A-Z])/g, '$1 $2').toUpperCase();
  }

  getRoleClass(role: string): string {
    return role.replace(/([a-z])([A-Z])/g, '$1-$2').replace(/\s+/g, '-').toLowerCase();
  }

  getStatusLabel(status: string): string {
    switch (status?.toUpperCase()) {
      case 'APPROVED':         return 'Approved';
      case 'REJECTED':         return 'Rejected';
      case 'PENDING_APPROVAL': return 'Pending';
      default:                 return status ?? '';
    }
  }

  getStatusClass(status: string): string {
    switch (status?.toUpperCase()) {
      case 'APPROVED':         return 'status-approved';
      case 'REJECTED':         return 'status-rejected';
      case 'PENDING_APPROVAL': return 'status-pending';
      default:                 return '';
    }
  }

  isPending(user: PendingUser): boolean {
    return user.status?.toUpperCase() === 'PENDING_APPROVAL';
  }

  isActingOn(userId: string): boolean { return this.activeUserId === userId; }

  // ── Actions ──────────────────────────────────────────────────────────
  approveUser(user: PendingUser): void {
    if (this.activeUserId) return;
    this.activeUserId = user.userId;
    this.errorMessage = '';

    this.superAdminService.approveUser(user.userId).subscribe({
      next: () => {
        this.showActionMessage(`${user.fullName} has been approved.`);
        this.activeUserId = null;
        this.loadUsers();
      },
      error: err => {
        this.errorMessage = err?.error?.detail ?? err?.error?.message ?? 'Unable to approve this user.';
        this.activeUserId = null;
        this.changeDetectorRef.detectChanges();
      }
    });
  }

  rejectUser(user: PendingUser): void {
    if (this.activeUserId) return;
    this.activeUserId = user.userId;
    this.errorMessage = '';

    this.superAdminService.rejectUser(user.userId).subscribe({
      next: () => {
        this.showActionMessage(`${user.fullName} has been rejected.`);
        this.activeUserId = null;
        this.loadUsers();
      },
      error: err => {
        this.errorMessage = err?.error?.detail ?? err?.error?.message ?? 'Unable to reject this user.';
        this.activeUserId = null;
        this.changeDetectorRef.detectChanges();
      }
    });
  }

  private showActionMessage(message: string): void {
    this.snackBar.open(message, 'Close', UsersComponent.actionSnackBarConfig);
  }

  // ── Data loading ─────────────────────────────────────────────────────
  private loadUsers(): void {
    if (this.isLoading) return;
    this.isLoading = true;
    this.errorMessage = '';

    const request: UserSearchRequest = {
      status:     this.selectedStatus === 'all' ? undefined : this.selectedStatus,
      role:       this.selectedRole   === 'all' ? undefined : this.selectedRole,
      searchTerm: this.searchTerm.trim() || undefined,
      pageNumber: this.currentPage,
      pageSize:   this.pageSize
    };

    this.superAdminService.searchUsers(request).subscribe({
      next: result => {
        this.users      = result.items ?? [];
        this.totalCount = result.totalCount ?? 0;
        // Rebuild available roles from current page for the dropdown
        const allRoles = this.users.map(u => u.role);
        this.availableRoles = [...new Set(allRoles)].sort((a, b) => a.localeCompare(b));
        this.isLoading = false;
        this.changeDetectorRef.detectChanges();
      },
      error: () => {
        this.errorMessage = 'Unable to load users right now.';
        this.isLoading = false;
        this.changeDetectorRef.detectChanges();
      }
    });
  }
}
