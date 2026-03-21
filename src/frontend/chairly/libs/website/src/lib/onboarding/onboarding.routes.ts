import { Routes } from '@angular/router';

export const onboardingRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./feature').then((m) => m.LandingPageComponent),
  },
  {
    path: 'prijzen',
    loadComponent: () => import('./feature').then((m) => m.PricingPageComponent),
  },
  {
    path: 'abonneren',
    loadComponent: () => import('./feature').then((m) => m.SubscribePageComponent),
  },
  {
    path: 'bevestiging',
    loadComponent: () => import('./feature').then((m) => m.ConfirmationPageComponent),
  },
];
