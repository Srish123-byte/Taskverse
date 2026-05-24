import { Component } from '@angular/core';
import { SessionActivityService } from '../../services/session/session-activity.service';

@Component({
  selector: 'app-session-inactivity-warning',
  standalone: false,
  templateUrl: './session-inactivity-warning.component.html',
  styleUrl: './session-inactivity-warning.component.scss'
})
export class SessionInactivityWarningComponent {
  constructor(public readonly sessionActivityService: SessionActivityService) {}
}
