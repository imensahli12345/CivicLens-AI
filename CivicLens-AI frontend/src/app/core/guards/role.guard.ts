import { inject } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const roleGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const allowedRoles: string[] = route.data['roles'] || [];
  const normalizedAllowedRoles = allowedRoles.map((role) => authService.normalizeRole(role));
  const userRole = authService.getUserRole();
  const normalizedUserRole = authService.normalizeRole(userRole);

  if (normalizedAllowedRoles.length === 0 || normalizedAllowedRoles.includes(normalizedUserRole)) {
    return true;
  }

  router.navigate([authService.getHomeRouteByRole(userRole)]);
  return false;
};
