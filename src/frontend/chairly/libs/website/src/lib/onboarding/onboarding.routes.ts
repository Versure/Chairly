import { Routes } from '@angular/router';

export const onboardingRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./feature').then((m) => m.LandingPageComponent),
  },
  {
    path: 'demo-aanvragen',
    loadComponent: () => import('./feature').then((m) => m.DemoRequestPageComponent),
  },
  {
    path: 'aanmelden',
    loadComponent: () => import('./feature').then((m) => m.SignUpPageComponent),
  },
  {
    path: 'bevestiging',
    loadComponent: () => import('./feature').then((m) => m.ConfirmationPageComponent),
  },
];
