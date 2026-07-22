import { Routes } from '@angular/router';
import { requireAccountGuard } from './core/auth.guard';

export const routes: Routes = [
  {
    path: 'sign-in',
    loadComponent: () => import('./features/auth/sign-in.page').then((module) => module.SignInPage),
    title: 'Sign in — Community Starter',
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./features/auth/register.page').then((module) => module.RegisterPage),
    title: 'Create account — Community Starter',
  },
  {
    path: 'verify',
    loadComponent: () => import('./features/auth/verify.page').then((module) => module.VerifyPage),
    title: 'Verify account — Community Starter',
  },
  {
    path: 'home',
    canActivate: [requireAccountGuard],
    loadComponent: () => import('./features/feed/home.page').then((module) => module.HomePage),
    title: 'Home — Community Starter',
  },
  {
    path: 'communities/new',
    canActivate: [requireAccountGuard],
    loadComponent: () =>
      import('./features/communities/create-community.page').then(
        (module) => module.CreateCommunityPage,
      ),
    title: 'Create community — Community Starter',
  },
  {
    path: 'communities/:communityId/moderation',
    canActivate: [requireAccountGuard],
    loadComponent: () =>
      import('./features/moderation/moderation.page').then((module) => module.ModerationPage),
    title: 'Moderation — Community Starter',
  },
  {
    path: 'messages',
    canActivate: [requireAccountGuard],
    data: { title: 'Messages', subsystem: 'messaging-realtime' },
    loadComponent: () =>
      import('./features/capabilities/capability.page').then((module) => module.CapabilityPage),
  },
  {
    path: 'events',
    canActivate: [requireAccountGuard],
    data: { title: 'Events', subsystem: 'community-events' },
    loadComponent: () =>
      import('./features/capabilities/capability.page').then((module) => module.CapabilityPage),
  },
  {
    path: 'notifications',
    canActivate: [requireAccountGuard],
    data: { title: 'Notifications', subsystem: 'notifications-delivery' },
    loadComponent: () =>
      import('./features/capabilities/capability.page').then((module) => module.CapabilityPage),
  },
  {
    path: 'profile',
    canActivate: [requireAccountGuard],
    data: { title: 'Profile and relationships', subsystem: 'profiles-relationships' },
    loadComponent: () =>
      import('./features/capabilities/capability.page').then((module) => module.CapabilityPage),
  },
  {
    path: 'settings',
    canActivate: [requireAccountGuard],
    data: { title: 'Account and privacy', subsystem: 'privacy-data-lifecycle' },
    loadComponent: () =>
      import('./features/capabilities/capability.page').then((module) => module.CapabilityPage),
  },
  {
    path: 'capabilities',
    canActivate: [requireAccountGuard],
    loadComponent: () =>
      import('./features/capabilities/catalog.page').then((module) => module.CatalogPage),
    title: 'Capabilities — Community Starter',
  },
  { path: '', pathMatch: 'full', redirectTo: 'home' },
  { path: '**', redirectTo: 'home' },
];
