import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { RoleType } from '../../../common/enums/role-type.enum';

@Component({
  selector: 'app-approval-status',
  standalone: false,
  templateUrl: './approval-status.component.html',
  styleUrl: './approval-status.component.scss'
})
export class ApprovalStatusComponent implements OnInit {
  role: RoleType | '' = '';
  status = '';
  title = 'Approval Pending';
  message = 'Your account is awaiting approval.';

  constructor(private readonly route: ActivatedRoute) {}

  ngOnInit(): void {
    this.route.queryParamMap.subscribe(params => {
      this.role = this.normalizeRole(params.get('role'));
      this.status = this.normalizeValue(params.get('status'));
      this.resolveContent();
    });
  }

  private resolveContent(): void {
    if (this.status === 'REJECTED') {
      this.title = 'Access Restricted';
      this.message = 'Your account is not allowed to sign in. Please contact the administrator.';
      return;
    }

    this.title = 'Approval Pending';

    if (this.status === 'PENDING_APPROVAL') {
      if (this.role === RoleType.CollegeAdmin) {
        this.message = 'Your account is awaiting approval from the super administrator.';
        return;
      }

      if (this.role === RoleType.Trainer || this.role === RoleType.Student) {
        this.message = 'Your account is awaiting approval from your college administrator.';
        return;
      }
    }

    if (this.role === RoleType.CollegeAdmin) {
      this.message = 'Your account is awaiting approval from the super administrator.';
      return;
    }

    if (this.role === RoleType.Trainer || this.role === RoleType.Student) {
      this.message = 'Your account is awaiting approval from your college administrator.';
      return;
    }

    this.message = 'Your account is awaiting approval.';
  }

  private normalizeValue(value: string | null): string {
    return (value ?? '').trim().toUpperCase();
  }

  private normalizeRole(value: string | null): RoleType | '' {
    switch ((value ?? '').trim().toLowerCase()) {
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
        return '';
    }
  }
}
