import { Injectable, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { ApiClient } from './api-client.service';
import { CurrentAccount } from './api.models';

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly api = inject(ApiClient);
  readonly account = signal<CurrentAccount | null>(null);

  load(): Observable<CurrentAccount> {
    return this.api.currentAccount().pipe(tap((account) => this.account.set(account)));
  }

  clear(): void {
    this.account.set(null);
  }
}
