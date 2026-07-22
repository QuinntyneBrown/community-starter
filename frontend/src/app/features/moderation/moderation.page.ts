import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ApiClient } from '../../core/api-client.service';
import { ProblemDetails } from '../../core/api.models';

@Component({
  selector: 'cs-moderation-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './moderation.page.html',
  styleUrl: './moderation.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ModerationPage {
  private readonly api = inject(ApiClient);
  private readonly route = inject(ActivatedRoute);
  protected readonly communityId = this.route.snapshot.paramMap.get('communityId') ?? '';
  protected readonly isSubmitting = signal(false);
  protected readonly problem = signal<ProblemDetails | null>(null);
  protected readonly result = signal<{ actionId: string; postVersion: number } | null>(null);
  protected readonly form = new FormGroup({
    caseId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    postId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    kind: new FormControl('hide-post', { nonNullable: true, validators: [Validators.required] }),
    rationale: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(4000)],
    }),
    expectedCaseVersion: new FormControl(0, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(0)],
    }),
    expectedPostVersion: new FormControl(0, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(0)],
    }),
  });

  protected submit(): void {
    if (this.form.invalid || this.isSubmitting()) return;
    const { caseId, ...input } = this.form.getRawValue();
    this.problem.set(null);
    this.result.set(null);
    this.isSubmitting.set(true);
    this.api
      .moderate(this.communityId, caseId, input)
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: (result) => this.result.set(result),
        error: (problem: ProblemDetails) => this.problem.set(problem),
      });
  }
}
