import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class WorkspaceStore {
  readonly communityId = signal(localStorage.getItem('cs.communityId'));
  readonly communityName = signal(localStorage.getItem('cs.communityName'));

  selectCommunity(id: string, name: string): void {
    localStorage.setItem('cs.communityId', id);
    localStorage.setItem('cs.communityName', name);
    this.communityId.set(id);
    this.communityName.set(name);
  }
}
