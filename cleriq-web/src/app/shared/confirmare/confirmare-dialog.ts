import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';

export interface DateConfirmare {
  titlu: string;
  mesaj: string;
  etichetaConfirmare?: string;
  periculos?: boolean;
}

@Component({
  selector: 'app-confirmare-dialog',
  imports: [MatDialogModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>{{ date.titlu }}</h2>
    <mat-dialog-content>{{ date.mesaj }}</mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Renunță</button>
      <button mat-flat-button [class.btn-periculos]="date.periculos" [mat-dialog-close]="true">
        {{ date.etichetaConfirmare ?? 'Confirmă' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    .btn-periculos {
      --mdc-filled-button-container-color: var(--mat-sys-error);
      --mdc-filled-button-label-text-color: var(--mat-sys-on-error);
      --mat-button-filled-container-color: var(--mat-sys-error);
      --mat-button-filled-label-text-color: var(--mat-sys-on-error);
    }
  `
})
export class ConfirmareDialog {
  readonly date = inject<DateConfirmare>(MAT_DIALOG_DATA);
}