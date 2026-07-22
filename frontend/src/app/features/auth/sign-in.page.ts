import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize, switchMap } from 'rxjs';
import { ApiClient } from '../../core/api-client.service';
import { ProblemDetails } from '../../core/api.models';
import { AuthStore } from '../../core/auth.store';

@Component({
  selector: 'cs-sign-in-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './sign-in.page.html',
  styleUrl: './auth-pages.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SignInPage {
  private readonly api = inject(ApiClient);
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);

  protected readonly isSubmitting = signal(false);
  protected readonly problem = signal<ProblemDetails | null>(null);
  protected readonly form = new FormGroup({
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email],
    }),
    password: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  protected submit(): void {
    if (this.form.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.problem.set(null);
    this.isSubmitting.set(true);
    const { email, password } = this.form.getRawValue();
    this.api
      .signIn(email, password)
      .pipe(
        switchMap(() => this.auth.load()),
        finalize(() => this.isSubmitting.set(false)),
      )
      .subscribe({
        next: () => void this.router.navigate(['/home']),
        error: (problem: ProblemDetails) => this.problem.set(problem),
      });
  }
}
