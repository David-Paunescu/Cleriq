import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);

  readonly seIncarca = signal(false);
  readonly eroare = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    parola: ['', Validators.required]
  });

  async autentifica(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.seIncarca.set(true);
    this.eroare.set(null);

    try {
      await this.auth.login(this.form.getRawValue());
      const redirect = this.route.snapshot.queryParamMap.get('redirect') ?? '/';
      this.router.navigateByUrl(redirect);
    } catch (err) {
      this.eroare.set(this.extrageMesaj(err));
    } finally {
      this.seIncarca.set(false);
    }
  }

  private extrageMesaj(err: unknown): string {
    if (err instanceof HttpErrorResponse) {
      if (err.status === 401)
        return typeof err.error === 'string' ? err.error : 'Email sau parolă greșite.';
      if (err.status === 0)
        return 'Serverul nu poate fi contactat.';
    }
    return 'A apărut o eroare neașteptată. Încearcă din nou.';
  }
}