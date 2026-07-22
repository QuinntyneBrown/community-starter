import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthStore } from './auth.store';

export const requireAccountGuard: CanActivateFn = () => {
  const auth = inject(AuthStore);
  const router = inject(Router);
  if (auth.account()) {
    return true;
  }
  return auth.load().pipe(
    map(() => true),
    catchError(() => of(router.createUrlTree(['/sign-in']))),
  );
};
