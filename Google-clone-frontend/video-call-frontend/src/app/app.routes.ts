import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./components/meeting/my-meetings/my-meetings.component')
      .then(m => m.MyMeetingsComponent),
    canActivate: [authGuard]
  },
  { 
    path: 'login', 
    loadComponent: () => import('./components/auth/login/login.component')
      .then(m => m.LoginComponent)
  },
  { 
    path: 'register', 
    loadComponent: () => import('./components/auth/register/register.component')
      .then(m => m.RegisterComponent)
  },
  { 
    path: 'create-meeting', 
    loadComponent: () => import('./components/meeting/create-meeting/create-meeting.component')
      .then(m => m.CreateMeetingComponent),
    canActivate: [authGuard]
  },
  { 
    path: 'join-meeting', 
    loadComponent: () => import('./components/meeting/join-meeting/join-meeting.component')
      .then(m => m.JoinMeetingComponent),
    canActivate: [authGuard]
  },
  { 
    path: 'my-meetings', 
    redirectTo: '',
    pathMatch: 'full'
  },
  { 
    path: 'meeting/:id', 
    loadComponent: () => import('./components/meeting/meeting-room/meeting-room.component')
      .then(m => m.MeetingRoomComponent),
    canActivate: [authGuard]
  },
  { path: '**', redirectTo: '' }
];
