import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Session } from '../../../common/services/session/session.service';
import { AccountService } from '../../../common/services/api/account.service';
import { RoleType } from '../../../common/enums/role-type.enum';
import { RouteAddress } from '../../../common/constants/routes.constants';
import { User } from '../../../common/models/user.model';
import { take } from 'rxjs/operators';

@Component({
  selector: 'app-role-director',
  standalone: false,
  templateUrl: './role-director.component.html',
  styleUrl: './role-director.component.scss'
})
export class RoleDirectorComponent implements OnInit {
  constructor(
    private readonly session: Session,
    private readonly accountService: AccountService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    // If the role is already in session (e.g. page refresh), dispatch immediately.
    // Otherwise fetch the profile from the API first.
    if (this.session.role) {
      this.dispatch(this.session.role);
    } else {
      this.accountService
        .getUserProfile()
        .pipe(take(1))
        .subscribe({
          next: (user: User) => {
            this.session.user      = user;
            this.session.userEmail = user.email;
            this.session.userId    = user.userId;
            this.session.role      = user.role;
            this.dispatch(user.role);
          },
          error: () => this.router.navigate([RouteAddress.Error])
        });
    }
  }

  private dispatch(role: RoleType): void {
    switch (role) {
      case RoleType.SuperAdmin:
        this.router.navigate([RouteAddress.SuperAdmin.Dashboard]);
        break;
      case RoleType.CollegeAdmin:
        this.router.navigate([RouteAddress.CollegeAdmin.Dashboard]);
        break;
      case RoleType.Trainer:
        this.router.navigate([RouteAddress.Trainer.Dashboard]);
        break;
      case RoleType.Student:
        this.router.navigate([RouteAddress.Student.Dashboard]);
        break;
      default:
        this.router.navigate([RouteAddress.Error]);
    }
  }
}
