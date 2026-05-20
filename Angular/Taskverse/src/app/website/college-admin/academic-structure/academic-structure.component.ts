import { ChangeDetectorRef, Component, DestroyRef, ElementRef, OnDestroy, OnInit, QueryList, ViewChildren, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, Validators } from '@angular/forms';
import { NavigationEnd, Router } from '@angular/router';
import { Subscription, filter, startWith } from 'rxjs';
import { finalize, take, timeout } from 'rxjs/operators';
import {
  ApprovedTrainer,
  AssignBatchTrainersRequest,
  ClassConfiguration,
  CollegeAdminService,
  CollegeBatchSummary,
  CollegeClassSummary
} from '../../../common/services/api/college-admin.service';
import {
  CLASS_OR_BATCH_NAME_HINT,
  CLASS_OR_BATCH_NAME_MAX_LENGTH,
  classOrBatchNameValidator
} from '../../../common/validators/class-batch-name-creation.validators';

interface BatchViewModel {
  batch?: CollegeBatchSummary;
  name: string;
  subtitle: string;
  assignedTrainerSummary: string;
  students: number;
  status: 'Active' | 'Pending';
  variant: 'live' | 'draft';
}

@Component({
  selector: 'app-college-admin-academic-structure',
  standalone: false,
  templateUrl: './academic-structure.component.html',
  styleUrl: './academic-structure.component.scss'
})
export class AcademicStructureComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  @ViewChildren('classBlock') private classBlocks!: QueryList<ElementRef<HTMLElement>>;
  readonly classNameHint = CLASS_OR_BATCH_NAME_HINT;
  readonly classNameMaxLength = CLASS_OR_BATCH_NAME_MAX_LENGTH;

  isLoading = true;
  isCreateClassOpen = false;
  isCreateBatchOpen = false;
  isTrainerAssignmentOpen = false;
  isTrainerDropdownOpen = false;
  isSubmittingClass = false;
  isSubmittingBatch = false;
  isLoadingApprovedTrainers = false;
  isSubmittingTrainerAssignment = false;
  isSuccessDialogOpen = false;
  errorMessage = '';
  createClassErrorMessage = '';
  createBatchErrorMessage = '';
  trainerAssignmentErrorMessage = '';
  successMessage = '';
  private hasBroughtFirstClassIntoView = false;
  private hasLoadedApprovedTrainers = false;
  private routeSubscription?: Subscription;
  approvedTrainers: ApprovedTrainer[] = [];
  initialSelectedTrainerIds = new Set<string>();
  selectedTrainerIds = new Set<string>();
  activeTrainerAssignmentClassId = '';
  activeTrainerAssignmentClassName = '';
  activeTrainerAssignmentBatchId = '';
  activeTrainerAssignmentBatchName = '';
  classConfiguration: ClassConfiguration = {
    totals: {
      totalClasses: 0,
      totalBatches: 0,
      totalStudents: 0,
      capacityUtilization: 0
    },
    classes: []
  };

  readonly yearOptions = Array.from({ length: 9 }, (_, index) => `${2024 + index}`);

  readonly createClassForm = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(2), classOrBatchNameValidator()]],
    academicYear: ['', [Validators.required]],
    description: ['']
  });

  readonly createBatchForm = this.fb.group({
    classId: ['', [Validators.required]],
    name: ['', [Validators.required, Validators.minLength(2), classOrBatchNameValidator()]],
    description: [''],
    capacity: [null as number | null, [Validators.required, Validators.min(1)]]
  });

  constructor(
    private readonly collegeAdminService: CollegeAdminService,
    private readonly router: Router,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.routeSubscription = this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(event => {
        if (event.urlAfterRedirects.endsWith('/college-admin/classes-management') &&
            (this.classConfiguration.classes.length === 0 || this.errorMessage)) {
          this.loadConfiguration();
        }
      });

    if (this.router.url.endsWith('/college-admin/classes-management')) {
      this.loadConfiguration();
    }
  }

  ngAfterViewInit(): void {
    this.classBlocks.changes
      .pipe(
        startWith(this.classBlocks),
        takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.bringFirstClassIntoView();
      });
  }

  ngOnDestroy(): void {
    this.routeSubscription?.unsubscribe();
  }

  get classes(): CollegeClassSummary[] {
    return this.classConfiguration.classes;
  }

  openCreateClassForm(): void {
    this.createClassErrorMessage = '';
    this.successMessage = '';
    this.isCreateClassOpen = true;
  }

  openCreateBatchForm(classId = ''): void {
    if (this.classConfiguration.classes.length === 0) {
      return;
    }

    this.createBatchErrorMessage = '';
    this.successMessage = '';
    this.isCreateClassOpen = false;
    this.closeTrainerAssignmentModal();
    this.isCreateBatchOpen = true;
    this.createBatchForm.reset({
      classId,
      name: '',
      description: '',
      capacity: null
    });
  }

  closeCreateClassForm(): void {
    this.isCreateClassOpen = false;
    this.isSubmittingClass = false;
    this.createClassErrorMessage = '';
    this.createClassForm.reset({
      name: '',
      academicYear: '',
      description: ''
    });
  }

  closeCreateBatchForm(): void {
    this.isCreateBatchOpen = false;
    this.isSubmittingBatch = false;
    this.createBatchErrorMessage = '';
    this.createBatchForm.reset({
      classId: '',
      name: '',
      description: '',
      capacity: null
    });
  }

  closeSuccessDialog(): void {
    this.isSuccessDialogOpen = false;
    this.successMessage = '';
  }

  openAssignTrainerModal(classItem: CollegeClassSummary, batch: CollegeBatchSummary): void {
    this.isCreateClassOpen = false;
    this.isCreateBatchOpen = false;
    this.isTrainerAssignmentOpen = true;
    this.isTrainerDropdownOpen = true;
    this.trainerAssignmentErrorMessage = '';
    this.successMessage = '';
    this.activeTrainerAssignmentClassId = classItem.classId;
    this.activeTrainerAssignmentClassName = classItem.name;
    this.activeTrainerAssignmentBatchId = batch.batchId;
    this.activeTrainerAssignmentBatchName = batch.name;
    this.initialSelectedTrainerIds = new Set(batch.assignedTrainers.map(trainer => trainer.trainerId));
    this.selectedTrainerIds = new Set(this.initialSelectedTrainerIds);

    if (!this.hasLoadedApprovedTrainers && !this.isLoadingApprovedTrainers) {
      this.loadApprovedTrainers(true);
    }
  }

  closeTrainerAssignmentModal(force = false): void {
    if (this.isSubmittingTrainerAssignment && !force) {
      return;
    }

    this.isTrainerAssignmentOpen = false;
    this.isTrainerDropdownOpen = false;
    this.isSubmittingTrainerAssignment = false;
    this.isLoadingApprovedTrainers = false;
    this.trainerAssignmentErrorMessage = '';
    this.initialSelectedTrainerIds = new Set<string>();
    this.selectedTrainerIds = new Set<string>();
    this.activeTrainerAssignmentClassId = '';
    this.activeTrainerAssignmentClassName = '';
    this.activeTrainerAssignmentBatchId = '';
    this.activeTrainerAssignmentBatchName = '';
  }

  submitCreateClass(): void {
    if (this.createClassForm.invalid) {
      this.createClassForm.markAllAsTouched();
      return;
    }

    const formValue = this.createClassForm.getRawValue();
    this.isSubmittingClass = true;
    this.createClassErrorMessage = '';
    this.successMessage = '';

    this.collegeAdminService.createClass({
      name: formValue.name?.trim() || '',
      academicYear: formValue.academicYear?.trim() || '',
      // The current API contract exposes this field as "department",
      // while the backend reads it back as class description.
      department: formValue.description?.trim() || undefined
    })
      .pipe(
        take(1),
        finalize(() => {
          this.isSubmittingClass = false;
        }))
      .subscribe({
        next: createdClass => {
          this.classConfiguration = {
            ...this.classConfiguration,
            classes: [createdClass, ...this.classConfiguration.classes]
          };
          this.recalculateTotals();
          this.successMessage = `Class "${createdClass.name}" was created successfully.`;
          this.closeCreateClassForm();
          this.isSuccessDialogOpen = true;
        },
        error: err => {
          this.createClassErrorMessage = err?.error?.message || 'Unable to create the class right now.';
        }
      });
  }

  submitCreateBatch(): void {
    if (this.createBatchForm.invalid) {
      this.createBatchForm.markAllAsTouched();
      return;
    }

    const formValue = this.createBatchForm.getRawValue();
    const classId = formValue.classId?.trim() || '';

    this.isSubmittingBatch = true;
    this.createBatchErrorMessage = '';
    this.successMessage = '';

    this.collegeAdminService.createBatch(classId, {
      name: formValue.name?.trim() || '',
      description: formValue.description?.trim() || undefined,
      capacity: Number(formValue.capacity) || undefined
    })
      .pipe(
        take(1),
        finalize(() => {
          this.isSubmittingBatch = false;
        }))
      .subscribe({
        next: createdBatch => {
          const selectedClass = this.classConfiguration.classes.find(item => item.classId === classId);
          if (selectedClass) {
            selectedClass.batches = [...selectedClass.batches, createdBatch].sort((left, right) => left.name.localeCompare(right.name));
            selectedClass.totalCapacity += createdBatch.capacity || 0;
          }

          this.recalculateTotals();
          this.successMessage = `Batch "${createdBatch.name}" was created successfully.`;
          this.closeCreateBatchForm();
          this.isSuccessDialogOpen = true;
        },
        error: err => {
          this.createBatchErrorMessage = err?.error?.message || 'Unable to create the batch right now.';
        }
      });
  }

  getCapacityPercent(classItem: CollegeClassSummary): number {
    if (!classItem.totalCapacity) {
      return 0;
    }

    return Math.min(100, Math.round((classItem.totalStudents / classItem.totalCapacity) * 100));
  }

  getCapacitySummary(classItem: CollegeClassSummary): string {
    return `${classItem.totalStudents}/${classItem.totalCapacity}`;
  }

  getBadge(classItem: CollegeClassSummary): string {
    return classItem.name
      .split(/\s+/)
      .map(token => token[0] ?? '')
      .join('')
      .slice(0, 2)
      .toUpperCase() || 'CL';
  }

  getBatchTokens(classItem: CollegeClassSummary): string[] {
    const tokens = classItem.batches.slice(0, 3).map(batch => batch.name);
    const remaining = classItem.batches.length - tokens.length;

    if (remaining > 0) {
      tokens.push(`+${remaining}`);
    }

    return tokens;
  }

  getBatchCards(classItem: CollegeClassSummary): BatchViewModel[] {
    const liveCards = classItem.batches.slice(0, 2).map(batch => this.mapBatchCard(batch));

    if (classItem.batches.length >= 3) {
      liveCards.push(this.mapBatchCard(classItem.batches[2]));
      return liveCards;
    }

    return [
      ...liveCards,
      {
        name: `Create ${classItem.batches.length === 0 ? 'First Batch' : 'Next Batch'}`,
        subtitle: 'Set up remaining students later',
        assignedTrainerSummary: '',
        students: 0,
        status: 'Pending',
        variant: 'draft'
      }
    ];
  }

  trackByClassId(_: number, item: CollegeClassSummary): string {
    return item.classId;
  }

  trackByBatchId(_: number, item: CollegeBatchSummary): string {
    return item.batchId;
  }

  trackByTrainerId(_: number, item: ApprovedTrainer): string {
    return item.trainerId;
  }

  toggleTrainerDropdown(): void {
    if (this.isLoadingApprovedTrainers || this.approvedTrainers.length === 0) {
      return;
    }

    this.isTrainerDropdownOpen = !this.isTrainerDropdownOpen;
  }

  toggleTrainerSelection(trainerId: string): void {
    this.trainerAssignmentErrorMessage = '';

    if (this.selectedTrainerIds.has(trainerId)) {
      this.selectedTrainerIds.delete(trainerId);
      return;
    }

    this.selectedTrainerIds.add(trainerId);
  }

  isTrainerSelected(trainerId: string): boolean {
    return this.selectedTrainerIds.has(trainerId);
  }

  getSelectedTrainerCount(): number {
    return this.selectedTrainerIds.size;
  }

  getTrainerDropdownLabel(): string {
    if (this.isLoadingApprovedTrainers) {
      return 'Loading approved trainers...';
    }

    const selectedCount = this.getSelectedTrainerCount();
    if (selectedCount > 0) {
      return `${selectedCount} trainer${selectedCount === 1 ? '' : 's'} selected`;
    }

    if (this.approvedTrainers.length === 0) {
      return 'No approved trainers available';
    }

    return 'Choose approved trainers';
  }

  getSelectedTrainerNames(): string[] {
    return this.approvedTrainers
      .filter(trainer => this.selectedTrainerIds.has(trainer.trainerId))
      .map(trainer => trainer.fullName);
  }

  getSelectedTrainers(): ApprovedTrainer[] {
    return this.approvedTrainers.filter(trainer => this.selectedTrainerIds.has(trainer.trainerId));
  }

  hasTrainerSelectionChanges(): boolean {
    if (this.initialSelectedTrainerIds.size !== this.selectedTrainerIds.size) {
      return true;
    }

    for (const trainerId of this.selectedTrainerIds) {
      if (!this.initialSelectedTrainerIds.has(trainerId)) {
        return true;
      }
    }

    return false;
  }

  submitTrainerAssignment(): void {
    if (this.isSubmittingTrainerAssignment) {
      return;
    }

    if (!this.activeTrainerAssignmentClassId || !this.activeTrainerAssignmentBatchId) {
      this.trainerAssignmentErrorMessage = 'Unable to identify the selected batch right now.';
      return;
    }

    const request: AssignBatchTrainersRequest = {
      trainerIds: Array.from(this.selectedTrainerIds)
    };

    this.isSubmittingTrainerAssignment = true;
    this.trainerAssignmentErrorMessage = '';

    this.collegeAdminService.assignBatchTrainers(
      this.activeTrainerAssignmentClassId,
      this.activeTrainerAssignmentBatchId,
      request)
      .pipe(
        take(1),
        finalize(() => {
          this.isSubmittingTrainerAssignment = false;
        }))
      .subscribe({
        next: updatedBatch => {
          this.replaceBatchSummary(updatedBatch);
          this.successMessage = `Trainer assignments were updated for "${updatedBatch.name}".`;
          this.closeTrainerAssignmentModal(true);
          this.isSuccessDialogOpen = true;
        },
        error: err => {
          this.trainerAssignmentErrorMessage = err?.error?.message || 'Unable to assign trainers right now.';
        }
      });
  }

  private mapBatchCard(batch: CollegeBatchSummary): BatchViewModel {
    return {
      batch,
      name: batch.name,
      subtitle: `Capacity: ${batch.capacity || 0} seats`,
      assignedTrainerSummary: batch.assignedTrainers.length > 0
        ? batch.assignedTrainers.map(trainer => trainer.fullName).join(', ')
        : 'Not assigned',
      students: batch.studentCount,
      status: 'Active',
      variant: 'live'
    };
  }

  private loadConfiguration(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.hasBroughtFirstClassIntoView = false;

    this.collegeAdminService.getClassConfiguration()
      .pipe(
        take(1),
        timeout(15000),
        finalize(() => {
          this.isLoading = false;
          this.changeDetectorRef.detectChanges();
        }))
      .subscribe({
        next: configuration => {
          this.classConfiguration = configuration;
          if (!this.hasLoadedApprovedTrainers && !this.isLoadingApprovedTrainers) {
            this.loadApprovedTrainers(false);
          }
          this.changeDetectorRef.detectChanges();
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
          this.errorMessage = err?.error?.message || 'Unable to load classes right now.';
          this.changeDetectorRef.detectChanges();
        }
      });
  }

  private loadApprovedTrainers(showError = true): void {
    this.isLoadingApprovedTrainers = true;
    if (showError) {
      this.trainerAssignmentErrorMessage = '';
    }

    this.collegeAdminService.getApprovedTrainers()
      .pipe(
        take(1),
        finalize(() => {
          this.isLoadingApprovedTrainers = false;
        }))
      .subscribe({
        next: trainers => {
          this.approvedTrainers = trainers;
          this.hasLoadedApprovedTrainers = true;
        },
        error: err => {
          this.approvedTrainers = [];
          this.hasLoadedApprovedTrainers = false;
          if (showError) {
            this.trainerAssignmentErrorMessage = err?.error?.message || 'Unable to load approved trainers right now.';
          }
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

  private replaceBatchSummary(updatedBatch: CollegeBatchSummary): void {
    this.classConfiguration = {
      ...this.classConfiguration,
      classes: this.classConfiguration.classes.map(classItem => {
        if (classItem.classId !== updatedBatch.classId) {
          return classItem;
        }

        return {
          ...classItem,
          batches: classItem.batches.map(batch => batch.batchId === updatedBatch.batchId ? updatedBatch : batch)
        };
      })
    };
  }

  private bringFirstClassIntoView(): void {
    if (this.hasBroughtFirstClassIntoView || this.classConfiguration.classes.length === 0) {
      return;
    }

    const firstClass = this.classBlocks?.first?.nativeElement;
    if (!firstClass) {
      return;
    }

    const firstClassTop = firstClass.getBoundingClientRect().top;
    const viewportBottomThreshold = window.innerHeight - 96;

    if (firstClassTop > viewportBottomThreshold) {
      firstClass.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    this.hasBroughtFirstClassIntoView = true;
  }
}
