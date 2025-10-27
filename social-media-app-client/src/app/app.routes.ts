import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/feed',
    pathMatch: 'full'
  },
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.AUTH_ROUTES)
  },
  {
    path: 'feed',
    loadChildren: () => import('./features/feed/feed.routes').then(m => m.FEED_ROUTES)
  },
  {
    path: 'users',
    loadChildren: () => import('./features/users/users.routes').then(m => m.USERS_ROUTES)
  },
  {
    path: 'chat',
    loadChildren: () => import('./features/chat/chat.routes').then(m => m.CHAT_ROUTES)
  },
  {
    path: '**',
    redirectTo: '/feed'
  }
];
