import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ApiClient } from '../../core/api-client.service';
import { ProblemDetails } from '../../core/api.models';

@Component({
  selector: 'cs-verify-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './verify.page.html',
  styleUrl: './auth-pages.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VerifyPage {
  private readonly api = inject(ApiClient);
  private readonly route = inject(ActivatedRoute);
  protected readonly isSubmitting = signal(false);
  protected readonly isComplete = signal(false);
  protected readonly problem = signal<ProblemDetails | null>(null);
  protected readonly form = new FormGroup({
    token: new FormControl(this.route.snapshot.queryParamMap.get('token') ?? '', {
      nonNullable: true,
      validators: [Validators.required],
    }),
  });

  protected submit(): void {
    if (this.form.invalid || this.isSubmitting()) return;
    this.problem.set(null);
    this.isSubmitting.set(true);
    this.api
      .verify(this.form.controls.token.value)
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => this.isComplete.set(true),
        error: (problem: ProblemDetails) => this.problem.set(problem),
      });
  }
}
