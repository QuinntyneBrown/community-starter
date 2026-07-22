import { HttpInterceptorFn } from '@angular/common/http';

const safeMethods = new Set(['GET', 'HEAD', 'OPTIONS']);

export const csrfInterceptor: HttpInterceptorFn = (request, next) => {
  let outgoing = request.clone({ withCredentials: true });
  if (!safeMethods.has(request.method.toUpperCase())) {
    const token = readCookie('cs_csrf');
    if (token) {
      outgoing = outgoing.clone({ setHeaders: { 'X-CSRF-Token': token } });
    }
  }
  return next(outgoing);
};

function readCookie(name: string): string | null {
  const prefix = `${encodeURIComponent(name)}=`;
  const value = document.cookie.split('; ').find((part) => part.startsWith(prefix));
  return value ? decodeURIComponent(value.slice(prefix.length)) : null;
}
