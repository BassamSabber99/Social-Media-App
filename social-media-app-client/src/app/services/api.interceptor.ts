import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';

export const apiInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const token = authService.getToken();

  let headers = req.headers.set('Accept', 'application/json');
  
  if (token) {
    headers = headers.set('Authorization', `Bearer ${token}`);
  }

  const cloned = req.clone({ headers });

  return next(cloned);
};

