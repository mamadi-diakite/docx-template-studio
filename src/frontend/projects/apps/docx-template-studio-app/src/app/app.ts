import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { ApiHealthService, AuthService, SessionStore } from '@docx-template-studio/shared/core';
import { Shell } from '@docx-template-studio/shared/ui';

@Component({
  selector: 'dts-root',
  imports: [RouterOutlet, RouterLink, Shell],
  template: `
    <dts-shell appName="Docx Template Studio" [apiStatus]="apiStatus()">
      <ng-container shellNav>
        @if (user(); as user) {
          <span class="text-slate-600" data-testid="user-name">{{ user.displayName }}</span>
          <a routerLink="/contracts" class="text-blue-600">Contracts</a>
          <a routerLink="/templates" class="text-blue-600">Templates</a>
          <a routerLink="/account/password" class="text-blue-600">Change password</a>
          <button type="button" class="text-blue-600" (click)="logOut()">Log out</button>
        } @else {
          <a routerLink="/login" class="text-blue-600">Log in</a>
          <a routerLink="/register" class="text-blue-600">Create an account</a>
        }
      </ng-container>
      <router-outlet />
    </dts-shell>
  `,
})
export class App {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly user = inject(SessionStore).user;
  protected readonly apiStatus = toSignal(inject(ApiHealthService).check(), {
    initialValue: 'checking',
  });

  protected logOut(): void {
    this.auth.logout();
    void this.router.navigateByUrl('/login');
  }
}
