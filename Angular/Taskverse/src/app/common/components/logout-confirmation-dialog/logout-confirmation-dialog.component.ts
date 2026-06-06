import { Component, EventEmitter, Output } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';

@Component({
  selector: 'app-logout-confirmation-dialog',
  standalone: false,
  templateUrl: './logout-confirmation-dialog.component.html',
  styleUrl: './logout-confirmation-dialog.component.scss'
})
export class LogoutConfirmationDialogComponent {
  @Output() readonly confirmed = new EventEmitter<void>();
  isProcessing = false;

  constructor(private readonly dialogRef: MatDialogRef<LogoutConfirmationDialogComponent, boolean>) {}

  confirm(): void {
    if (this.isProcessing) {
      return;
    }

    this.confirmed.emit();
  }

  cancel(): void {
    if (this.isProcessing) {
      return;
    }

    this.dialogRef.close(false);
  }
}
