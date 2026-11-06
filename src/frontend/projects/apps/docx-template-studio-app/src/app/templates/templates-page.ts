import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  DOCUMENT_MODULES,
  FormSubmission,
  LoadState,
  MAX_TEMPLATE_BYTES,
  moduleLabel,
  Template,
  TemplatesService,
  toFormErrors,
  withLoadState,
} from '@docx-template-studio/shared/core';
import { FieldErrors } from '@docx-template-studio/shared/ui';
import { switchMap } from 'rxjs';
import { formatFileSize } from '../shared/file-size';

@Component({
  selector: 'dts-templates-page',
  imports: [DatePipe, ReactiveFormsModule, FieldErrors],
  template: `
    <section class="space-y-8">
      <h1 class="text-2xl font-bold tracking-tight">Templates</h1>

      <form
        class="space-y-4 rounded-xl border border-slate-200 bg-white p-6 shadow-sm"
        [formGroup]="form"
        (ngSubmit)="upload(fileInput)"
        novalidate
      >
        <h2 class="text-lg font-semibold">Add a template</h2>
        <p class="text-sm text-slate-600">
          Use a Word .docx file (10 MB max). Each content control with a tag becomes a field key the
          app can fill.
        </p>

        @if (uploaded(); as template) {
          <p class="alert-info" role="status" data-testid="uploaded">
            "{{ template.name }}" was added.
            @if (template.fieldKeys.length > 0) {
              Field keys found: {{ template.fieldKeys.join(', ') }}.
            } @else {
              No field keys found: add tagged content controls in Word to fill this template.
            }
          </p>
        }
        @if (submission.errors().form; as message) {
          <p class="alert-error" role="alert">{{ message }}</p>
        }

        <div class="grid gap-4 sm:grid-cols-2">
          <div>
            <label class="form-label" for="module">Module</label>
            <select id="module" class="form-input" formControlName="module">
              <option value="">Choose a module</option>
              @for (option of modules; track option.value) {
                <option [value]="option.value">{{ option.label }}</option>
              }
            </select>
            <dts-field-errors [messages]="submission.messagesFor('module')" />
          </div>
          <div>
            <label class="form-label" for="name">Name (optional)</label>
            <input id="name" class="form-input" formControlName="name" maxlength="200" />
            <p class="mt-1 text-xs text-slate-500">The file name is used when empty.</p>
            <dts-field-errors [messages]="submission.messagesFor('name')" />
          </div>
        </div>

        <div>
          <label class="form-label" for="file">Word file</label>
          <input
            #fileInput
            id="file"
            class="mt-1 block text-sm"
            type="file"
            accept=".docx,application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            (change)="choose(fileInput)"
          />
          <dts-field-errors [messages]="fileMessages()" />
        </div>

        <button class="button-primary sm:w-auto" type="submit" [disabled]="submission.pending()">
          Upload template
        </button>
      </form>

      <section class="space-y-4" aria-labelledby="list-title">
        <div class="flex flex-wrap items-end justify-between gap-4">
          <h2 id="list-title" class="text-lg font-semibold">Your templates</h2>
          <div>
            <label class="form-label" for="moduleFilter">Show</label>
            <select id="moduleFilter" class="form-input" (change)="filter($event)">
              <option value="">All modules</option>
              @for (option of modules; track option.value) {
                <option [value]="option.value">{{ option.label }}</option>
              }
            </select>
          </div>
        </div>

        @if (actionError(); as message) {
          <p class="alert-error" role="alert">{{ message }}</p>
        }

        @let current = state();
        @if (current.status === 'loading') {
          <p class="text-slate-600" role="status">Loading the templates…</p>
        } @else if (current.status === 'ready') {
          <ul class="space-y-3" data-testid="templates">
            @for (template of current.value; track template.id) {
              <li class="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
                <div class="flex flex-wrap items-start justify-between gap-4">
                  <div>
                    <p class="font-medium" data-testid="name">{{ template.name }}</p>
                    <p class="text-sm text-slate-500" data-testid="details">
                      {{ label(template.module) }} · {{ template.fileName }} ·
                      {{ size(template.sizeBytes) }} · Added
                      {{ template.uploadedAtUtc | date: 'mediumDate' }}
                    </p>
                  </div>
                  <div class="flex gap-3 text-sm">
                    <button
                      type="button"
                      class="link"
                      [attr.aria-label]="'Download ' + template.name"
                      (click)="download(template)"
                    >
                      Download
                    </button>
                    <button
                      type="button"
                      class="font-medium text-rose-700 hover:underline disabled:opacity-60"
                      [attr.aria-label]="'Delete ' + template.name"
                      [disabled]="busy()"
                      (click)="remove(template)"
                    >
                      Delete
                    </button>
                  </div>
                </div>
                @if (template.fieldKeys.length > 0) {
                  <ul class="mt-3 flex flex-wrap gap-2" aria-label="Field keys">
                    @for (key of template.fieldKeys; track key) {
                      <li>
                        <code class="rounded bg-slate-100 px-2 py-0.5 text-xs">{{ key }}</code>
                      </li>
                    }
                  </ul>
                } @else {
                  <p class="mt-3 text-xs text-slate-500">No field keys.</p>
                }
              </li>
            } @empty {
              <li class="text-slate-600">No templates yet.</li>
            }
          </ul>
        } @else {
          <p class="alert-error" role="alert">
            The templates could not be loaded. Please try again later.
          </p>
        }
      </section>
    </section>
  `,
})
export class TemplatesPage {
  private readonly templates = inject(TemplatesService);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly query = signal({ module: '' });

  protected readonly modules = DOCUMENT_MODULES;
  protected readonly label = moduleLabel;
  protected readonly size = formatFileSize;
  protected readonly busy = signal(false);
  protected readonly actionError = signal<string | null>(null);
  protected readonly fileProblem = signal<string | null>(null);
  protected readonly uploaded = signal<Template | null>(null);
  protected readonly form = this.formBuilder.group({
    module: ['', Validators.required],
    name: [''],
    file: this.formBuilder.control<File | null>(null, Validators.required),
  });
  protected readonly submission = new FormSubmission(this.form, {
    module: 'Choose a module.',
    file: 'Choose a .docx file.',
  });
  protected readonly state = toSignal(
    toObservable(this.query).pipe(
      switchMap(({ module }) => withLoadState(this.templates.list(module))),
    ),
    { initialValue: { status: 'loading' } as LoadState<Template[]> },
  );

  protected fileMessages(): readonly string[] {
    const problem = this.fileProblem();
    return problem ? [problem] : this.submission.messagesFor('file');
  }

  protected choose(input: HTMLInputElement): void {
    const file = input.files?.[0] ?? null;
    const problem = file ? checkTemplate(file) : null;
    this.fileProblem.set(problem);
    this.form.controls.file.setValue(problem ? null : file);
    this.form.controls.file.markAsTouched();
    if (problem) {
      input.value = '';
    }
  }

  protected upload(input: HTMLInputElement): void {
    const { module, name, file } = this.form.getRawValue();
    this.uploaded.set(null);
    this.submission.submit(
      () => this.templates.upload({ module, name, file: file! }),
      (template) => {
        this.uploaded.set(template);
        this.form.reset();
        input.value = '';
        this.reload();
      },
    );
  }

  protected filter(event: Event): void {
    this.query.set({ module: (event.target as HTMLSelectElement).value });
  }

  protected download(template: Template): void {
    this.actionError.set(null);
    this.templates.download(template.id).subscribe({
      next: (blob) => saveFile(blob, template.fileName),
      error: (error: unknown) => this.actionError.set(toFormErrors(error).form),
    });
  }

  protected remove(template: Template): void {
    if (!confirm(`Delete the template "${template.name}"?`)) {
      return;
    }

    this.busy.set(true);
    this.actionError.set(null);
    this.templates.delete(template.id).subscribe({
      next: () => {
        this.busy.set(false);
        this.reload();
      },
      error: (error: unknown) => {
        this.busy.set(false);
        this.actionError.set(toFormErrors(error).form);
      },
    });
  }

  private reload(): void {
    this.query.update((query) => ({ ...query }));
  }
}

function checkTemplate(file: File): string | null {
  if (!file.name.toLowerCase().endsWith('.docx')) {
    return 'Only Word .docx files can be used as templates.';
  }

  return file.size > MAX_TEMPLATE_BYTES ? 'The file is larger than 10 MB.' : null;
}

function saveFile(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  link.click();
  URL.revokeObjectURL(url);
}
