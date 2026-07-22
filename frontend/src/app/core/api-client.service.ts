import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { Community, CurrentAccount, CursorPage, Post, ProblemDetails } from './api.models';

@Injectable({ providedIn: 'root' })
export class ApiClient {
  private readonly http = inject(HttpClient);

  register(
    email: string,
    password: string,
  ): Observable<{ accountId: string; verificationToken?: string }> {
    return this.request(() =>
      this.http.post<{ accountId: string; verificationToken?: string }>('/api/accounts', {
        email,
        password,
      }),
    );
  }

  verify(token: string): Observable<void> {
    return this.request(() => this.http.post<void>('/api/accounts/verify', { token }));
  }

  signIn(email: string, password: string): Observable<{ accountId: string; expiresAt: string }> {
    return this.request(() =>
      this.http.post<{ accountId: string; expiresAt: string }>('/api/sessions', {
        email,
        password,
        deviceLabel: navigator.userAgent.slice(0, 120),
      }),
    );
  }

  currentAccount(): Observable<CurrentAccount> {
    return this.request(() => this.http.get<CurrentAccount>('/api/me'));
  }

  createCommunity(input: {
    slug: string;
    name: string;
    description: string;
  }): Observable<Community> {
    return this.request(() => this.http.post<Community>('/api/communities', input));
  }

  feed(communityId: string, cursor?: string): Observable<CursorPage<Post>> {
    const params = cursor ? new HttpParams().set('cursor', cursor) : undefined;
    return this.request(() =>
      this.http.get<CursorPage<Post>>(`/api/communities/${communityId}/feed`, { params }),
    );
  }

  publishPost(communityId: string, body: string): Observable<Post> {
    return this.request(() =>
      this.http.post<Post>(`/api/communities/${communityId}/posts`, { body }),
    );
  }

  react(communityId: string, postId: string, kind: string): Observable<void> {
    return this.request(() =>
      this.http.post<void>(`/api/communities/${communityId}/posts/${postId}/reactions`, { kind }),
    );
  }

  report(
    communityId: string,
    postId: string,
    reason: string,
  ): Observable<{ reportId: string; caseId: string }> {
    return this.request(() =>
      this.http.post<{ reportId: string; caseId: string }>(
        `/api/communities/${communityId}/posts/${postId}/reports`,
        { reason },
      ),
    );
  }

  moderate(
    communityId: string,
    caseId: string,
    input: {
      postId: string;
      kind: string;
      rationale: string;
      expectedCaseVersion: number;
      expectedPostVersion: number;
    },
  ): Observable<{ actionId: string; postVersion: number }> {
    return this.request(() =>
      this.http.post<{ actionId: string; postVersion: number }>(
        `/api/communities/${communityId}/moderation-cases/${caseId}/actions`,
        input,
      ),
    );
  }

  private request<T>(factory: () => Observable<T>): Observable<T> {
    return factory().pipe(
      catchError((error: HttpErrorResponse) => throwError(() => toProblem(error))),
    );
  }
}

function toProblem(error: HttpErrorResponse): ProblemDetails {
  const body = error.error as Partial<ProblemDetails> | null;
  return {
    type: body?.type ?? 'about:blank',
    title: body?.title ?? 'The request could not be completed.',
    status: body?.status ?? error.status,
    code: body?.code,
    traceId: body?.traceId,
    errors: body?.errors,
  };
}
