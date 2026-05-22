import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-delete-action-button',
  standalone: false,
  templateUrl: './delete-action-button.component.html',
  styleUrl: './delete-action-button.component.scss'
})
export class DeleteActionButtonComponent {
  @Input() label = 'Delete';
  @Input() ariaLabel = 'Delete';
  @Input() appearance: 'icon' | 'button' = 'button';
  @Input() disabled = false;
  @Input() loading = false;

  @Output() deleteClick = new EventEmitter<MouseEvent>();

  handleClick(event: MouseEvent): void {
    if (this.disabled || this.loading) {
      return;
    }

    this.deleteClick.emit(event);
  }
}
