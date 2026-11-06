export const CONTRACT_TYPES = [
  { value: 'Permanent', label: 'Permanent' },
  { value: 'FixedTerm', label: 'Fixed-term' },
  { value: 'Internship', label: 'Internship' },
  { value: 'Apprenticeship', label: 'Apprenticeship' },
] as const;

export type ContractType = (typeof CONTRACT_TYPES)[number]['value'];

export function contractTypeLabel(type: string): string {
  return CONTRACT_TYPES.find((option) => option.value === type)?.label ?? type;
}

export interface ContractInput {
  readonly employeeFirstName: string;
  readonly employeeLastName: string;
  readonly position: string;
  readonly type: string;
  /** ISO date, e.g. 2026-01-15. */
  readonly startDate: string;
  /** Empty for permanent contracts, required for the other types. */
  readonly endDate: string | null;
  readonly grossMonthlySalary: number;
}

export interface Contract extends ContractInput {
  readonly id: string;
  readonly createdAtUtc: string;
  readonly updatedAtUtc: string;
}

export interface PagedResponse<T> {
  readonly items: readonly T[];
  /** 1-based page number. */
  readonly page: number;
  readonly pageSize: number;
  readonly totalCount: number;
}

export function pageCount(response: PagedResponse<unknown>): number {
  return Math.max(1, Math.ceil(response.totalCount / response.pageSize));
}
