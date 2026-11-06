import { Routes } from '@angular/router';
import { authGuard, guestGuard } from '@docx-template-studio/shared/core';
import { ChangePasswordPage } from './auth/change-password-page';
import { LoginPage } from './auth/login-page';
import { RegisterPage } from './auth/register-page';
import { ContractFormPage } from './contracts/contract-form-page';
import { ContractsPage } from './contracts/contracts-page';
import { HomePage } from './home/home-page';
import { TemplatesPage } from './templates/templates-page';

export const routes: Routes = [
  { path: '', component: HomePage, title: 'Docx Template Studio' },
  {
    path: 'login',
    component: LoginPage,
    canActivate: [guestGuard],
    title: 'Log in · Docx Template Studio',
  },
  {
    path: 'register',
    component: RegisterPage,
    canActivate: [guestGuard],
    title: 'Create an account · Docx Template Studio',
  },
  {
    path: 'account/password',
    component: ChangePasswordPage,
    canActivate: [authGuard],
    title: 'Change password · Docx Template Studio',
  },
  {
    path: 'contracts',
    component: ContractsPage,
    canActivate: [authGuard],
    title: 'Contracts · Docx Template Studio',
  },
  {
    path: 'contracts/new',
    component: ContractFormPage,
    canActivate: [authGuard],
    title: 'New contract · Docx Template Studio',
  },
  {
    path: 'contracts/:id',
    component: ContractFormPage,
    canActivate: [authGuard],
    title: 'Edit contract · Docx Template Studio',
  },
  {
    path: 'templates',
    component: TemplatesPage,
    canActivate: [authGuard],
    title: 'Templates · Docx Template Studio',
  },
  { path: '**', redirectTo: '' },
];
