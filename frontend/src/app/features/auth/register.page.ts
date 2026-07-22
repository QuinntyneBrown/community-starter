import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ApiClient } from '../../core/api-client.service';
import { ProblemDetails } from '../../core/api.models';

@Component({
  selector: 'cs-register-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.page.html',
  styleUrl: './auth-pages.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterPage {
  private readonly api = inject(ApiClient);
  protected readonly isSubmitting = signal(false);
  protected readonly problem = signal<ProblemDetails | null>(null);
  protected readonly isComplete = signal(false);
  protected readonly developmentToken = signal<string | null>(null);
  protected readonly form = new FormGroup({
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email],
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(12), Validators.maxLength(256)],
    }),
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
      .register(email, password)
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: (result) => {
          this.developmentToken.set(result.verificationToken ?? null);
          this.isComplete.set(true);
        },
        error: (problem: ProblemDetails) => this.problem.set(problem),
      });
  }
}
