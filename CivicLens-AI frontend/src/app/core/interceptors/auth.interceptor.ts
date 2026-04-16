import { HttpInterceptorFn } from '@angular/common/http';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = localStorage.getItem('access_token');
  
  // Ne pas ajouter le token si on est sur login ou register
  if (req.url.includes('/auth/login') || req.url.includes('/auth/register')) {
    return next(req);
  }

  if (token) {
    // S'assurer que le token ne contient pas déjà "Bearer "
    const authHeader = token.startsWith('Bearer ') ? token : `Bearer ${token}`;
    
    const cloned = req.clone({
      setHeaders: {
        Authorization: authHeader
      }
    });
    return next(cloned);
  }
  return next(req);
};