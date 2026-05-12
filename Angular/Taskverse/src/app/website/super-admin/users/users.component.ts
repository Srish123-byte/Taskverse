import { Component, OnInit } from '@angular/core';
import { PendingUser } from '../../../common/models/super-admin.model';
import { SuperAdminService } from '../../../common/services/api/super-admin.service';

@Component({
  selector: 'app-super-admin-users',
  standalone: false,
  templateUrl: './users.component.html',
  styleUrl: './users.component.scss'
})
export class UsersComponent implements OnInit {
  pendingUsers: PendingUser[] = [];
  isLoading = false;
  errorMessage = '';

  constructor(private readonly superAdminService: SuperAdminService) {}

  ngOnInit(): void {
    this.loadPendingUsers();
  }

  trackByUserId(_: number, user: PendingUser): string {
    return user.userId;
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

  private loadPendingUsers(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.superAdminService.getPendingUsers().subscribe({
      next: users => {
        this.pendingUsers = users;
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Unable to load pending users right now.';
        this.isLoading = false;
      }
    });
  }
}
