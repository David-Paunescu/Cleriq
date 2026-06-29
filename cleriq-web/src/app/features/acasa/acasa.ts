import { Component, computed, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { AuthService } from '../../core/auth/auth.service';
import { HclUrgentWidget } from '../hcl/hcl-urgent-widget/hcl-urgent-widget';

@Component({
  selector: 'app-acasa',
  imports: [MatCardModule, HclUrgentWidget],
  templateUrl: './acasa.html'
})
export class Acasa {
  readonly auth = inject(AuthService);
  readonly esteAdminSauSecretar = computed(() => this.auth.areOricareRol('Admin', 'Secretar'));
}
