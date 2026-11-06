export const DOCUMENT_MODULES = [
  { value: 'Contracts', label: 'Employment contracts' },
  { value: 'TrainingAgreements', label: 'Training agreements' },
  { value: 'MissionOrders', label: 'Mission orders' },
] as const;

export function moduleLabel(module: string): string {
  return DOCUMENT_MODULES.find((option) => option.value === module)?.label ?? module;
}

export interface Template {
  readonly id: string;
  readonly module: string;
  readonly name: string;
  readonly fileName: string;
  readonly sizeBytes: number;
  /** Tags of the content controls found in the template, in document order. */
  readonly fieldKeys: readonly string[];
  readonly uploadedAtUtc: string;
}

export interface TemplateUpload {
  readonly module: string;
  /** Defaults to the file name on the API side when empty. */
  readonly name: string;
  readonly file: File;
}

/** Same limit as the API, checked first in the browser to avoid a useless upload. */
export const MAX_TEMPLATE_BYTES = 10 * 1024 * 1024;
