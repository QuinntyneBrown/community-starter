import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ApiClient } from '../../core/api-client.service';
import { Post, ProblemDetails } from '../../core/api.models';
import { WorkspaceStore } from '../../core/workspace.store';

@Component({
  selector: 'cs-home-page',
  imports: [DatePipe, ReactiveFormsModule, RouterLink],
  templateUrl: './home.page.html',
  styleUrl: './home.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomePage implements OnInit {
  private readonly api = inject(ApiClient);
  protected readonly workspace = inject(WorkspaceStore);
  protected readonly posts = signal<readonly Post[]>([]);
  protected readonly nextCursor = signal<string | null>(null);
  protected readonly isLoading = signal(false);
  protected readonly isPublishing = signal(false);
  protected readonly problem = signal<ProblemDetails | null>(null);
  protected readonly reportingPostId = signal<string | null>(null);
  protected readonly reportConfirmation = signal<string | null>(null);
  protected readonly composer = new FormGroup({
    body: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(20_000)],
    }),
  });
  protected readonly reportForm = new FormGroup({
    reason: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(2_000)],
    }),
  });

  ngOnInit(): void {
    if (this.workspace.communityId()) this.load(false);
  }

  protected publish(): void {
    const communityId = this.workspace.communityId();
    if (!communityId || this.composer.invalid || this.isPublishing()) return;
    this.problem.set(null);
    this.isPublishing.set(true);
    this.api
      .publishPost(communityId, this.composer.controls.body.value)
      .pipe(finalize(() => this.isPublishing.set(false)))
      .subscribe({
        next: (post) => {
          this.posts.update((posts) => [post, ...posts]);
          this.composer.reset();
        },
        error: (problem: ProblemDetails) => this.problem.set(problem),
      });
  }

  protected load(append: boolean): void {
    const communityId = this.workspace.communityId();
    if (!communityId || this.isLoading()) return;
    this.problem.set(null);
    this.isLoading.set(true);
    this.api
      .feed(communityId, append ? (this.nextCursor() ?? undefined) : undefined)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (page) => {
          this.posts.update((posts) => (append ? [...posts, ...page.items] : page.items));
          this.nextCursor.set(page.nextCursor);
        },
        error: (problem: ProblemDetails) => this.problem.set(problem),
      });
  }

  protected react(post: Post, kind: string): void {
    const communityId = this.workspace.communityId();
    if (!communityId) return;
    this.api.react(communityId, post.id, kind).subscribe({
      next: () =>
        this.posts.update((posts) =>
          posts.map((current) =>
            current.id === post.id
              ? { ...current, reactionCount: current.reactionCount + 1 }
              : current,
          ),
        ),
      error: (problem: ProblemDetails) => this.problem.set(problem),
    });
  }

  protected openReport(postId: string): void {
    this.reportForm.reset();
    this.reportingPostId.set(postId);
    this.reportConfirmation.set(null);
  }

  protected submitReport(): void {
    const communityId = this.workspace.communityId();
    const postId = this.reportingPostId();
    if (!communityId || !postId || this.reportForm.invalid) return;
    this.api.report(communityId, postId, this.reportForm.controls.reason.value).subscribe({
      next: (result) => {
        this.reportingPostId.set(null);
        this.reportConfirmation.set(`Report received. Case ${result.caseId} is ready for review.`);
      },
      error: (problem: ProblemDetails) => this.problem.set(problem),
    });
  }
}
