import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { CollegeAdminService } from '../../../common/services/api/college-admin.service';
import { PendingUser, UserActionRequest } from '../../../common/models/super-admin.model';

type RoleFilter = 'all' | 'student' | 'trainer';
type ActionType = 'approve' | 'reject';

interface ConfirmState {
  active: boolean;
  userId: string;
  userName: string;
  action: ActionType;
}

@Component({
  selector: 'app-college-admin-user-approvals',
  standalone: false,
  templateUrl: './user-approvals.component.html',
  styleUrl: './user-approvals.component.scss'
})
export class UserApprovalsComponent implements OnInit, OnDestroy {
  pendingUsers: PendingUser[] = [];
  isLoading = true;
  errorMessage: string | null = null;
  activeFilter: RoleFilter = 'all';
  actionInProgress: string | null = null; // userId of the user being actioned

  confirm: ConfirmState = {
    active: false,
    userId: '',
    userName: '',
    action: 'approve'
  };

  private readonly subscriptions = new Subscription();

  constructor(private readonly collegeAdminService: CollegeAdminService) {}

  ngOnInit(): void {
    this.loadPendingUsers();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  get filteredUsers(): PendingUser[] {
    if (this.activeFilter === 'all') return this.pendingUsers;
    return this.pendingUsers.filter(
      u => u.role.toLowerCase() === this.activeFilter
    );
  }

  get studentCount(): number {
    return this.pendingUsers.filter(u => u.role.toLowerCase() === 'student').length;
  }

  get trainerCount(): number {
    return this.pendingUsers.filter(u => u.role.toLowerCase() === 'trainer').length;
  }

  setFilter(filter: RoleFilter): void {
    this.activeFilter = filter;
  }

  promptAction(user: PendingUser, action: ActionType): void {
    this.confirm = {
      active: true,
      userId: user.userId,
      userName: user.fullName,
      action
    };
  }

  cancelConfirm(): void {
    this.confirm = { active: false, userId: '', userName: '', action: 'approve' };
  }

  confirmAction(): void {
    const { userId, action } = this.confirm;
    this.cancelConfirm();
    this.actionInProgress = userId;

    const request: UserActionRequest = {};
    const action$ = action === 'approve'
      ? this.collegeAdminService.approveUser(userId, request)
      : this.collegeAdminService.rejectUser(userId, request);

    this.subscriptions.add(
      action$.subscribe({
        next: () => {
          this.pendingUsers = this.pendingUsers.filter(u => u.userId !== userId);
          this.actionInProgress = null;
        },
        error: () => {
          this.errorMessage = `Failed to ${action} the user. Please try again.`;
          this.actionInProgress = null;
        }
      })
    );
  }

  getInitials(fullName: string): string {
    return fullName
      .split(' ')
      .slice(0, 2)
      .map(n => n[0]?.toUpperCase() ?? '')
      .join('');
  }

  getRoleClass(role: string): string {
    const r = role.toLowerCase();
    if (r === 'student') return 'chip-student';
    if (r === 'trainer') return 'chip-trainer';
    return 'chip-default';
  }

  private loadPendingUsers(): void {
    this.isLoading = true;
    this.errorMessage = null;
    this.subscriptions.add(
      this.collegeAdminService.getPendingUsers().subscribe({
        next: users => {
          this.pendingUsers = users;
          this.isLoading = false;
        },
        error: () => {
          this.errorMessage = 'Unable to load pending users. Please refresh.';
          this.isLoading = false;
        }
      })
    );
  }
}
