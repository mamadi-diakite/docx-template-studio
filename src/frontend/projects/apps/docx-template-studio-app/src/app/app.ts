import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterOutlet } from '@angular/router';
import { ApiHealthService } from '@docx-template-studio/shared/core';
import { Shell } from '@docx-template-studio/shared/ui';

@Component({
  selector: 'dts-root',
  imports: [RouterOutlet, Shell],
  template: `
    <dts-shell appName="Docx Template Studio" [apiStatus]="apiStatus()">
      <router-outlet />
    </dts-shell>
  `,
})
export class App {
  protected readonly apiStatus = toSignal(inject(ApiHealthService).check(), {
    initialValue: 'checking',
  });
}
