import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { finalize, take } from 'rxjs/operators';
import {
  ClassConfiguration,
  CollegeAdminService,
  CollegeBatchSummary,
  CollegeClassSummary,
  CreateCollegeBatchRequest,
  CreateCollegeClassRequest
} from '../../../common/services/api/college-admin.service';
import { Session } from '../../../common/services/session/session.service';

type ClassSortOption = 'latest' | 'name' | 'students';

interface MetricCard {
  label: string;
  value: string;
  icon: string;
  accent: 'blue' | 'gold' | 'sky' | 'teal';
}

@Component({
  selector: 'app-college-admin-academic-structure',
  standalone: false,
  templateUrl: './academic-structure.component.html',
  styleUrl: './academic-structure.component.scss'
})
export class AcademicStructureComponent implements OnInit {
  private readonly fb = inject(FormBuilder);

  isLoading = true;
  isSubmittingClass = false;
  isSubmittingBatch = false;
  isClassFormOpen = false;
  selectedDepartment = 'All Classes';
  selectedSort: ClassSortOption = 'latest';
  batchFormClassId: string | null = null;
  errorMessage = '';
  successMessage = '';
  classConfiguration: ClassConfiguration = {
    totals: {
      totalClasses: 0,
      totalBatches: 0,
      totalStudents: 0,
      capacityUtilization: 0
    },
    classes: []
  };

  readonly sortOptions: { value: ClassSortOption; label: string }[] = [
    { value: 'latest', label: 'Latest Created' },
    { value: 'name', label: 'Class Name' },
    { value: 'students', label: 'Students' }
  ];

  readonly classForm = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    academicYear: ['', [Validators.required, Validators.minLength(4)]],
    department: ['', [Validators.required, Validators.minLength(2)]]
  });

  readonly batchForm = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(1)]],
    capacity: [null as number | null, [Validators.min(0)]]
  });

  constructor(
    private readonly collegeAdminService: CollegeAdminService,
    private readonly session: Session
  ) {}

  ngOnInit(): void {
    this.loadConfiguration();
  }

  get metricCards(): MetricCard[] {
    return [
      { label: 'Total Classes', value: `${this.classConfiguration.totals.totalClasses}`, icon: 'school', accent: 'blue' },
      { label: 'Total Batches', value: `${this.classConfiguration.totals.totalBatches}`, icon: 'groups', accent: 'gold' },
      { label: 'Total Students', value: this.formatNumber(this.classConfiguration.totals.totalStudents), icon: 'person', accent: 'sky' },
      { label: 'Capacity Utilization', value: `${this.classConfiguration.totals.capacityUtilization}%`, icon: 'verified', accent: 'teal' }
    ];
  }

  get departmentFilters(): string[] {
    const departments = this.classConfiguration.classes
      .map(item => item.department?.trim())
      .filter((item): item is string => !!item);

    return ['All Classes', ...new Set(departments)];
  }

  get visibleClasses(): CollegeClassSummary[] {
    const items = this.classConfiguration.classes.filter(item =>
      this.selectedDepartment === 'All Classes'
        ? true
        : (item.department ?? '').toLowerCase() === this.selectedDepartment.toLowerCase());

    return [...items].sort((left, right) => {
      switch (this.selectedSort) {
        case 'name':
          return left.name.localeCompare(right.name);
        case 'students':
          return right.totalStudents - left.totalStudents;
        case 'latest':
        default:
          return new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime();
      }
    });
  }

  get institutionName(): string {
    return this.session.user?.collegeName?.trim() || 'your college';
  }

  selectDepartment(department: string): void {
    this.selectedDepartment = department;
  }

  toggleClassForm(): void {
    this.successMessage = '';
    this.errorMessage = '';

    this.isClassFormOpen = !this.isClassFormOpen;

    if (!this.isClassFormOpen) {
      this.classForm.reset();
      return;
    }

    this.classForm.reset({
      name: '',
      academicYear: '',
      department: ''
    });
  }

  openBatchForm(classId: string): void {
    this.successMessage = '';
    this.errorMessage = '';
    this.batchFormClassId = this.batchFormClassId === classId ? null : classId;
    this.batchForm.reset({
      name: '',
      capacity: null
    });
  }

  createClass(): void {
    if (this.classForm.invalid) {
      this.classForm.markAllAsTouched();
      return;
    }

    this.isSubmittingClass = true;
    this.errorMessage = '';
    this.successMessage = '';

    const request = this.classForm.getRawValue() as CreateCollegeClassRequest;
    this.collegeAdminService.createClass(request)
      .pipe(
        take(1),
        finalize(() => {
          this.isSubmittingClass = false;
        }))
      .subscribe({
        next: createdClass => {
          this.classConfiguration.classes = [createdClass, ...this.classConfiguration.classes];
          this.recalculateTotals();
          this.classForm.reset({
            name: '',
            academicYear: '',
            department: ''
          });
          this.isClassFormOpen = false;
          this.successMessage = `Class "${createdClass.name}" was created successfully.`;
        },
        error: err => {
          this.errorMessage = err?.error?.message || 'Unable to create the class right now.';
        }
      });
  }

  createBatch(classItem: CollegeClassSummary): void {
    if (this.batchForm.invalid) {
      this.batchForm.markAllAsTouched();
      return;
    }

    this.isSubmittingBatch = true;
    this.errorMessage = '';
    this.successMessage = '';

    const request = this.batchForm.getRawValue() as CreateCollegeBatchRequest;
    this.collegeAdminService.createBatch(classItem.classId, request)
      .pipe(
        take(1),
        finalize(() => {
          this.isSubmittingBatch = false;
        }))
      .subscribe({
        next: createdBatch => {
          const targetClass = this.classConfiguration.classes.find(item => item.classId === classItem.classId);
          if (targetClass) {
            targetClass.batches = [...targetClass.batches, createdBatch].sort((left, right) => left.name.localeCompare(right.name));
            targetClass.totalCapacity += createdBatch.capacity;
          }

          this.recalculateTotals();
          this.batchFormClassId = null;
          this.batchForm.reset({
            name: '',
            capacity: null
          });
          this.successMessage = `Batch "${createdBatch.name}" was created for ${classItem.name}.`;
        },
        error: err => {
          this.errorMessage = err?.error?.message || 'Unable to create the batch right now.';
        }
      });
  }

  trackByClassId(_: number, item: CollegeClassSummary): string {
    return item.classId;
  }

  trackByBatchId(_: number, item: CollegeBatchSummary): string {
    return item.batchId;
  }

  getCapacityUsage(item: CollegeClassSummary): number {
    if (!item.totalCapacity) {
      return 0;
    }

    return Math.min(100, Math.round((item.totalStudents / item.totalCapacity) * 100));
  }

  isBatchFormOpenFor(classId: string): boolean {
    return this.batchFormClassId === classId;
  }

  private loadConfiguration(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.collegeAdminService.getClassConfiguration()
      .pipe(
        take(1),
        finalize(() => {
          this.isLoading = false;
        }))
      .subscribe({
        next: configuration => {
          this.classConfiguration = configuration;
          if (!this.departmentFilters.includes(this.selectedDepartment)) {
            this.selectedDepartment = 'All Classes';
          }
        },
        error: err => {
          this.classConfiguration = {
            totals: {
              totalClasses: 0,
              totalBatches: 0,
              totalStudents: 0,
              capacityUtilization: 0
            },
            classes: []
          };
          this.errorMessage = err?.error?.message || 'Unable to load classes and batches right now.';
        }
      });
  }

  private recalculateTotals(): void {
    const totalClasses = this.classConfiguration.classes.length;
    const totalBatches = this.classConfiguration.classes.reduce((sum, item) => sum + item.batches.length, 0);
    const totalStudents = this.classConfiguration.classes.reduce((sum, item) => sum + item.totalStudents, 0);
    const totalCapacity = this.classConfiguration.classes.reduce((sum, item) => sum + item.totalCapacity, 0);

    this.classConfiguration = {
      ...this.classConfiguration,
      classes: [...this.classConfiguration.classes],
      totals: {
        totalClasses,
        totalBatches,
        totalStudents,
        capacityUtilization: totalCapacity > 0 ? Math.round((totalStudents / totalCapacity) * 100) : 0
      }
    };
  }

  private formatNumber(value: number): string {
    return new Intl.NumberFormat('en-US').format(value);
  }
}
