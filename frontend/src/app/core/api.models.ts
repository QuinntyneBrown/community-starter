export interface ProblemDetails {
  readonly type: string;
  readonly title: string;
  readonly status: number;
  readonly code?: string;
  readonly traceId?: string;
  readonly errors?: Readonly<Record<string, readonly string[]>>;
}

export interface CurrentAccount {
  readonly id: string;
  readonly email: string;
  readonly status: string;
  readonly locale: string;
  readonly timeZone: string;
}

export interface Community {
  readonly id: string;
  readonly slug: string;
  readonly name: string;
  readonly description: string;
  readonly accessMode: 'Open' | 'Gated' | 'InvitationOnly';
  readonly isPubliclyListed: boolean;
  readonly version: number;
}

export interface Post {
  readonly id: string;
  readonly communityId: string;
  readonly authorAccountId: string;
  readonly body: string;
  readonly status: string;
  readonly publishedAt: string | null;
  readonly version: number;
  readonly reactionCount: number;
}

export interface CursorPage<T> {
  readonly items: readonly T[];
  readonly nextCursor: string | null;
}
