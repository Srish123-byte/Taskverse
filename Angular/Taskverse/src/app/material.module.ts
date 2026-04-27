import { NgModule } from '@angular/core';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatChipsModule } from '@angular/material/chips';
import { MatNativeDateModule } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialogModule } from '@angular/material/dialog';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatRadioModule } from '@angular/material/radio';
import { MatSelectModule } from '@angular/material/select';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatSortModule } from '@angular/material/sort';
import { MatStepperModule } from '@angular/material/stepper';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatTooltipModule } from '@angular/material/tooltip';

const materialModules = [
  MatInputModule,
  MatIconModule,
  MatRadioModule,
  MatSelectModule,
  MatMenuModule,
  MatCheckboxModule,
  MatSidenavModule,
  MatPaginatorModule,
  MatTableModule,
  MatExpansionModule,
  MatDatepickerModule,
  MatNativeDateModule,
  MatSortModule,
  MatProgressSpinnerModule,
  MatStepperModule,
  MatChipsModule,
  MatSnackBarModule,
  MatDialogModule,
  MatProgressBarModule,
  MatButtonModule,
  MatFormFieldModule,
  MatTooltipModule
];

@NgModule({
  imports: materialModules,
  exports: materialModules
})
export class MaterialModule {}
