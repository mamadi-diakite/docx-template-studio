import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  Contract,
  CONTRACT_TYPES,
  ContractInput,
  ContractsService,
  FormSubmission,
  LoadState,
  withLoadState,
} from '@docx-template-studio/shared/core';
import { FieldErrors } from '@docx-template-studio/shared/ui';

/** Creates a contract on /contracts/new, edits it on /contracts/:id. */
@Component({
  selector: 'dts-contract-form-page',
  imports: [ReactiveFormsModule, RouterLink, FieldErrors],
  template: `
    @switch (status()) {
      @case ('loading') {
        <p class="text-slate-600" role="status">Loading the contract…</p>
      }
      @case ('missing') {
        <p class="alert-info">This contract does not exist. It may have been deleted.</p>
      }
      @case ('error') {
        <p class="alert-error" role="alert">
          The contract could not be loaded. Please try again later.
        </p>
      }
      @default {
        <form class="form-card max-w-2xl" [formGroup]="form" (ngSubmit)="save()" novalidate>
          <h1 class="text-2xl font-bold tracking-tight">
            {{ id ? 'Edit contract' : 'New contract' }}
          </h1>

          @if (submission.errors().form; as message) {
            <p class="alert-error" role="alert">{{ message }}</p>
          }

          <div class="grid gap-6 sm:grid-cols-2">
            <div>
              <label class="form-label" for="employeeFirstName">First name</label>
              <input
                id="employeeFirstName"
                class="form-input"
                formControlName="employeeFirstName"
                maxlength="100"
              />
              <dts-field-errors [messages]="submission.messagesFor('employeeFirstName')" />
            </div>
            <div>
              <label class="form-label" for="employeeLastName">Last name</label>
              <input
                id="employeeLastName"
                class="form-input"
                formControlName="employeeLastName"
                maxlength="100"
              />
              <dts-field-errors [messages]="submission.messagesFor('employeeLastName')" />
            </div>
          </div>

          <div>
            <label class="form-label" for="position">Position</label>
            <input id="position" class="form-input" formControlName="position" maxlength="150" />
            <dts-field-errors [messages]="submission.messagesFor('position')" />
          </div>

          <div>
            <label class="form-label" for="type">Contract type</label>
            <select id="type" class="form-input" formControlName="type">
              @for (option of types; track option.value) {
                <option [value]="option.value">{{ option.label }}</option>
              }
            </select>
            <dts-field-errors [messages]="submission.messagesFor('type')" />
          </div>

          <div class="grid gap-6 sm:grid-cols-2">
            <div>
              <label class="form-label" for="startDate">Start date</label>
              <input id="startDate" class="form-input" type="date" formControlName="startDate" />
              <dts-field-errors [messages]="submission.messagesFor('startDate')" />
            </div>
            <div>
              <label class="form-label" for="endDate">End date</label>
              <input id="endDate" class="form-input" type="date" formControlName="endDate" />
              <p class="mt-1 text-xs text-slate-500">Leave empty for a permanent contract.</p>
              <dts-field-errors [messages]="submission.messagesFor('endDate')" />
            </div>
          </div>

          <div>
            <label class="form-label" for="grossMonthlySalary">Gross monthly salary (EUR)</label>
            <input
              id="grossMonthlySalary"
              class="form-input"
              type="number"
              min="0"
              step="0.01"
              formControlName="grossMonthlySalary"
            />
            <dts-field-errors [messages]="submission.messagesFor('grossMonthlySalary')" />
          </div>

          <button class="button-primary" type="submit" [disabled]="submission.pending()">
            Save contract
          </button>
          <p class="text-center text-sm">
            <a routerLink="/contracts" class="link">Back to the contracts</a>
          </p>
        </form>
      }
    }
  `,
})
export class ContractFormPage {
  private readonly contracts = inject(ContractsService);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(NonNullableFormBuilder);

  protected readonly id = inject(ActivatedRoute).snapshot.paramMap.get('id');
  protected readonly types = CONTRACT_TYPES;
  protected readonly status = signal<LoadState<Contract>['status']>(this.id ? 'loading' : 'ready');
  protected readonly form = this.formBuilder.group({
    employeeFirstName: ['', Validators.required],
    employeeLastName: ['', Validators.required],
    position: ['', Validators.required],
    type: ['Permanent', Validators.required],
    startDate: ['', Validators.required],
    endDate: [''],
    grossMonthlySalary: this.formBuilder.control<number | null>(null, Validators.required),
  });
  protected readonly submission = new FormSubmission(this.form, {
    employeeFirstName: 'Enter the first name.',
    employeeLastName: 'Enter the last name.',
    position: 'Enter the position.',
    type: 'Choose a contract type.',
    startDate: 'Enter the start date.',
    grossMonthlySalary: 'Enter a gross monthly salary of 0 or more.',
  });

  constructor() {
    if (this.id) {
      withLoadState(this.contracts.get(this.id)).subscribe((state) => {
        if (state.status === 'ready') {
          const contract = state.value;
          this.form.setValue({
            employeeFirstName: contract.employeeFirstName,
            employeeLastName: contract.employeeLastName,
            position: contract.position,
            type: contract.type,
            startDate: contract.startDate,
            endDate: contract.endDate ?? '',
            grossMonthlySalary: contract.grossMonthlySalary,
          });
        }
        this.status.set(state.status);
      });
    }
  }

  protected save(): void {
    const values = this.form.getRawValue();
    const input: ContractInput = {
      ...values,
      endDate: values.endDate || null,
      grossMonthlySalary: values.grossMonthlySalary!,
    };
    this.submission.submit(
      () => (this.id ? this.contracts.update(this.id, input) : this.contracts.create(input)),
      () => void this.router.navigateByUrl('/contracts'),
    );
  }
}
