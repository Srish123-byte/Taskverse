import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-info-tooltip',
  standalone: true,
  imports: [CommonModule, MatTooltipModule, MatIconModule],
  templateUrl: './info-tooltip.component.html',
  styleUrl: './info-tooltip.component.scss'
})
export class InfoTooltipComponent {
  @Input() tooltipText = '';
}
