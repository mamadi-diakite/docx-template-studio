import { Component } from '@angular/core';

@Component({
  selector: 'dts-home-page',
  template: `
    <section class="max-w-2xl space-y-4">
      <h1 class="text-3xl font-bold tracking-tight">Your Word templates, filled for you</h1>
      <p class="text-lg text-slate-600">
        Upload your own .docx templates, pick a record, and get the filled document with your layout
        and branding kept.
      </p>
    </section>
  `,
})
export class HomePage {}
