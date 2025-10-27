import { Routes } from '@angular/router';
import { FeedPageComponent } from './feed-page/feed-page.component';
import { authGuard } from '../../guards/auth.guard';

export const FEED_ROUTES: Routes = [
  {
    path: '',
    component: FeedPageComponent,
    canActivate: [authGuard]
  }
];

