import { CanDeactivateFn } from '@angular/router';
import { Observable } from 'rxjs';

export interface CanDeactivateComponent {
  canDeactivate: () => Observable<boolean> | boolean;
}

export const unsavedChangesGuard: CanDeactivateFn<CanDeactivateComponent> = (component) => {
  return component.canDeactivate();
};
