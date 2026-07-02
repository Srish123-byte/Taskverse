import { Component, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable } from 'rxjs';
import { AssessmentCreatorComponent } from '../../../common/components/assessment-creator/assessment-creator.component';
import { CanDeactivateComponent } from '../../../common/guards/unsaved-changes.guard';

@Component({
  selector: 'app-trainer-new-assessment',
  standalone: false,
  templateUrl: './new-assessment.component.html',
  styleUrl: './new-assessment.component.scss'
})
export class NewAssessmentComponent implements OnInit, CanDeactivateComponent {
  @ViewChild(AssessmentCreatorComponent) assessmentCreator?: AssessmentCreatorComponent;

  isCodingRoute = false;

  constructor(
    private readonly activatedRoute: ActivatedRoute,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    const url = this.activatedRoute.snapshot.url;
    this.isCodingRoute = url[url.length - 1]?.path === 'coding';
  }

  canDeactivate(): Observable<boolean> | boolean {
    return this.assessmentCreator?.canDeactivate() ?? true;
  }

  goBack(): void {
    void this.router.navigateByUrl('/trainer/assessments-management');
  }
}
