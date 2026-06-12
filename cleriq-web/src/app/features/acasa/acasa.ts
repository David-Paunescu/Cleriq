import { Component, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-acasa',
  imports: [MatCardModule],
  templateUrl: './acasa.html'
})
export class Acasa {
  readonly auth = inject(AuthService);
}