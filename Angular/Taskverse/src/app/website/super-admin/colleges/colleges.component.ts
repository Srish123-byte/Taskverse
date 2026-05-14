import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { Subscription, filter } from 'rxjs';
import { finalize, take, timeout } from 'rxjs/operators';
import { CollegeSearchResult } from '../../../common/models/super-admin.model';
import { SuperAdminService } from '../../../common/services/api/super-admin.service';

@Component({
  selector: 'app-super-admin-colleges',
  standalone: false,
  templateUrl: './colleges.component.html',
  styleUrl: './colleges.component.scss'
})
export class CollegesComponent implements OnInit, OnDestroy {
  readonly statusTabs = ['All', 'Pending', 'Suspended'];

  colleges: CollegeSearchResult[] = [];
  searchTerm = '';
  activeStatus = 'All';
  isLoading = false;
  errorMessage = '';
  actionMessage = '';
  private routeSubscription?: Subscription;

  constructor(
    private readonly superAdminService: SuperAdminService,
    private readonly router: Router,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.routeSubscription = this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(event => {
        if (event.urlAfterRedirects.endsWith('/super-admin/colleges') && (this.colleges.length === 0 || this.errorMessage)) {
          this.searchColleges();
        }
      });

    if (this.router.url.endsWith('/super-admin/colleges')) {
      this.searchColleges();
    }
  }

  ngOnDestroy(): void {
    this.routeSubscription?.unsubscribe();
  }

  trackByCollegeId(_: number, college: CollegeSearchResult): string {
    return college.collegeId;
  }

  setStatusTab(tab: string): void {
    if (this.activeStatus === tab) {
      return;
    }

    this.activeStatus = tab;
    this.searchColleges();
  }

  onSearchTermChange(): void {
    this.searchColleges();
  }

  getLocationLabel(college: CollegeSearchResult): string {
    return [college.city, college.state].filter(Boolean).join(', ') || 'Region not set';
  }

  getStatusClass(status: string): string {
    return status.trim().toLowerCase().replace(/\s+/g, '-');
  }

  getCollegeIcon(index: number): string {
    const icons = ['account_balance', 'architecture', 'science'];
    return icons[index % icons.length];
  }

  private searchColleges(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.actionMessage = '';

    this.superAdminService.searchColleges({
      query: this.searchTerm.trim() || undefined,
      status: this.activeStatus.toLowerCase()
    })
      .pipe(
        take(1),
        timeout(15000),
        finalize(() => {
          this.isLoading = false;
          this.changeDetectorRef.detectChanges();
        })
      )
      .subscribe({
        next: colleges => {
          this.colleges = colleges;
          this.changeDetectorRef.detectChanges();
        },
        error: () => {
          this.errorMessage = 'Unable to load colleges right now.';
          this.changeDetectorRef.detectChanges();
        }
      });
  }
}
