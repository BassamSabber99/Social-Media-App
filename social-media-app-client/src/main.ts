import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';
import { environment } from './environment';

bootstrapApplication(AppComponent, appConfig)
  .catch((err) => {
    if (!environment.production) {
      console.error('Angular bootstrap failed', err);
    }
  });
