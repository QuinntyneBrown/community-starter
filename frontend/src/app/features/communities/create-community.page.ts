import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ApiClient } from '../../core/api-client.service';
import { ProblemDetails } from '../../core/api.models';
import { WorkspaceStore } from '../../core/workspace.store';

@Component({
  selector: 'cs-create-community-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './create-community.page.html',
  styleUrl: './create-community.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CreateCommunityPage {
  private readonly api = inject(ApiClient);
  private readonly workspace = inject(WorkspaceStore);
  private readonly router = inject(Router);
  protected readonly isSubmitting = signal(false);
  protected readonly problem = signal<ProblemDetails | null>(null);
  protected readonly form = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(120)],
    }),
    slug: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/^[a-z0-9-]{3,64}$/)],
    }),
    description: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(2000)],
    }),
  });

  protected submit(): void {
    if (this.form.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }
    this.problem.set(null);
    this.isSubmitting.set(true);
    this.api
      .createCommunity(this.form.getRawValue())
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: (community) => {
          this.workspace.selectCommunity(community.id, community.name);
          void this.router.navigate(['/home']);
        },
        error: (problem: ProblemDetails) => this.problem.set(problem),
      });
  }
}
