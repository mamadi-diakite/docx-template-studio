import { Routes } from '@angular/router';
import { HomePage } from './home/home-page';

export const routes: Routes = [
  { path: '', component: HomePage, title: 'Docx Template Studio' },
  { path: '**', redirectTo: '' },
];
