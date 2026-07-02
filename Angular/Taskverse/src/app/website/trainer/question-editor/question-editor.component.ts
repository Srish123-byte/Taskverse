import { Component, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable } from 'rxjs';
import { CanDeactivateComponent } from '../../../common/guards/unsaved-changes.guard';
import { QuestionEditorComponent } from '../../../common/components/question-editor/question-editor.component';

@Component({
  selector: 'app-trainer-question-editor-page',
  standalone: false,
  templateUrl: './question-editor.component.html',
  styleUrl: './question-editor.component.scss'
})
export class QuestionEditorPageComponent implements OnInit, CanDeactivateComponent {
  @ViewChild(QuestionEditorComponent) questionEditor?: QuestionEditorComponent;

  isCodingRoute = false;

  constructor(
    private readonly activatedRoute: ActivatedRoute,
    private readonly router: Router
  ) {}

  canDeactivate(): Observable<boolean> | boolean {
    return this.questionEditor?.canDeactivate() ?? true;
  }

  ngOnInit(): void {
    const url = this.activatedRoute.snapshot.url;
    this.isCodingRoute = url[url.length - 1]?.path === 'coding';
  }

  goBack(): void {
    void this.router.navigateByUrl('/trainer/questions-management');
  }
}
