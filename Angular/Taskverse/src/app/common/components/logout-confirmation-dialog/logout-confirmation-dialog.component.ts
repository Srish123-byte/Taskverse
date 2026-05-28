import { Component } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';

@Component({
  selector: 'app-logout-confirmation-dialog',
  standalone: false,
  templateUrl: './logout-confirmation-dialog.component.html',
  styleUrl: './logout-confirmation-dialog.component.scss'
})
export class LogoutConfirmationDialogComponent {
  constructor(private readonly dialogRef: MatDialogRef<LogoutConfirmationDialogComponent, boolean>) {}

  confirm(): void {
    this.dialogRef.close(true);
  }

  cancel(): void {
    this.dialogRef.close(false);
  }
}
