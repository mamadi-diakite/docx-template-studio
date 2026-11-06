import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  Contract,
  ContractsService,
  contractTypeLabel,
  LoadState,
  PagedResponse,
  pageCount,
  toFormErrors,
  withLoadState,
} from '@docx-template-studio/shared/core';
import { switchMap } from 'rxjs';

@Component({
  selector: 'dts-contracts-page',
  imports: [CurrencyPipe, DatePipe, ReactiveFormsModule, RouterLink],
  template: `
    <section class="space-y-6">
      <div class="flex items-center justify-between gap-4">
        <h1 class="text-2xl font-bold tracking-tight">Employment contracts</h1>
        <a routerLink="/contracts/new" class="button-link">New contract</a>
      </div>

      <form class="flex gap-2" role="search" [formGroup]="searchForm" (ngSubmit)="search()">
        <label class="sr-only" for="search">Search contracts</label>
        <input
          id="search"
          class="form-input mt-0"
          type="search"
          placeholder="Search by name or position"
          formControlName="text"
        />
        <button class="button-secondary" type="submit">Search</button>
      </form>

      @if (actionError(); as message) {
        <p class="alert-error" role="alert">{{ message }}</p>
      }

      @let current = state();
      @if (current.status === 'loading') {
        <p class="text-slate-600" role="status">Loading the contracts…</p>
      } @else if (current.status === 'ready') {
        @let result = current.value;
        @if (result.items.length === 0) {
          <p class="text-slate-600" data-testid="empty">No contracts found.</p>
        } @else {
          <div class="overflow-x-auto rounded-xl border border-slate-200 bg-white shadow-sm">
            <table class="min-w-full divide-y divide-slate-200 text-sm">
              <thead class="bg-slate-50 text-left text-slate-600">
                <tr>
                  <th class="px-4 py-3 font-medium">Employee</th>
                  <th class="px-4 py-3 font-medium">Position</th>
                  <th class="px-4 py-3 font-medium">Type</th>
                  <th class="px-4 py-3 font-medium">Dates</th>
                  <th class="px-4 py-3 text-right font-medium">Gross monthly salary</th>
                  <th class="px-4 py-3"><span class="sr-only">Actions</span></th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-200">
                @for (contract of result.items; track contract.id) {
                  <tr>
                    <td class="px-4 py-3 font-medium">{{ fullName(contract) }}</td>
                    <td class="px-4 py-3">{{ contract.position }}</td>
                    <td class="px-4 py-3">{{ typeLabel(contract.type) }}</td>
                    <td class="px-4 py-3">
                      {{ contract.startDate | date: 'mediumDate' }}
                      @if (contract.endDate) {
                        – {{ contract.endDate | date: 'mediumDate' }}
                      }
                    </td>
                    <td class="px-4 py-3 text-right">
                      {{ contract.grossMonthlySalary | currency: 'EUR' }}
                    </td>
                    <td class="px-4 py-3 text-right whitespace-nowrap">
                      <a [routerLink]="['/contracts', contract.id]" class="link">Edit</a>
                      <button
                        type="button"
                        class="ml-3 font-medium text-rose-700 hover:underline disabled:opacity-60"
                        [attr.aria-label]="'Delete the contract of ' + fullName(contract)"
                        [disabled]="busy()"
                        (click)="remove(contract)"
                      >
                        Delete
                      </button>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>

          <nav class="flex items-center justify-between text-sm" aria-label="Pages">
            <button
              type="button"
              class="button-secondary"
              [disabled]="result.page <= 1"
              (click)="goTo(result.page - 1)"
            >
              Previous
            </button>
            <span class="text-slate-600" data-testid="page"
              >Page {{ result.page }} of {{ pages(result) }} ·
              {{ result.totalCount }} contracts</span
            >
            <button
              type="button"
              class="button-secondary"
              [disabled]="result.page >= pages(result)"
              (click)="goTo(result.page + 1)"
            >
              Next
            </button>
          </nav>
        }
      } @else {
        <p class="alert-error" role="alert">
          The contracts could not be loaded. Please try again later.
        </p>
      }
    </section>
  `,
})
export class ContractsPage {
  private readonly contracts = inject(ContractsService);
  private readonly query = signal({ search: '', page: 1 });

  protected readonly searchForm = new FormGroup({
    text: new FormControl('', { nonNullable: true }),
  });
  protected readonly busy = signal(false);
  protected readonly actionError = signal<string | null>(null);
  protected readonly state = toSignal(
    toObservable(this.query).pipe(
      switchMap(({ search, page }) => withLoadState(this.contracts.search(search, page))),
    ),
    { initialValue: { status: 'loading' } as LoadState<PagedResponse<Contract>> },
  );

  protected readonly typeLabel = contractTypeLabel;
  protected readonly pages = pageCount;

  protected fullName(contract: Contract): string {
    return `${contract.employeeFirstName} ${contract.employeeLastName}`;
  }

  protected search(): void {
    this.query.set({ search: this.searchForm.controls.text.value, page: 1 });
  }

  protected goTo(page: number): void {
    this.query.update((query) => ({ ...query, page }));
  }

  protected remove(contract: Contract): void {
    if (!confirm(`Delete the contract of ${this.fullName(contract)}?`)) {
      return;
    }

    this.busy.set(true);
    this.actionError.set(null);
    this.contracts.delete(contract.id).subscribe({
      next: () => {
        this.busy.set(false);
        this.query.update((query) => ({ ...query }));
      },
      error: (error: unknown) => {
        this.busy.set(false);
        this.actionError.set(toFormErrors(error).form);
      },
    });
  }
}
