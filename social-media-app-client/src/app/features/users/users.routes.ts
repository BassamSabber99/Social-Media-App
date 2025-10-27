import { Routes } from '@angular/router';
import { authGuard } from '../../guards/auth.guard';

export const USERS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./user-discovery/user-discovery.component').then(m => m.UserDiscoveryComponent),
    canActivate: [authGuard]
  }
];

