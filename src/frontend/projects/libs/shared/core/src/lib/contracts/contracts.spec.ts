import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideAppEnvironment } from '@docx-template-studio/shared/environment';
import { ContractInput, contractTypeLabel, pageCount } from './contracts.models';
import { ContractsService } from './contracts.service';

const input: ContractInput = {
  employeeFirstName: 'Leo',
  employeeLastName: 'Martin',
  position: 'Accountant',
  type: 'Permanent',
  startDate: '2026-01-05',
  endDate: null,
  grossMonthlySalary: 3200,
};

describe('contract helpers', () => {
  it('labels the contract types', () => {
    expect(contractTypeLabel('FixedTerm')).toBe('Fixed-term');
    expect(contractTypeLabel('Unknown')).toBe('Unknown');
  });

  it('counts at least one page', () => {
    expect(pageCount({ items: [], page: 1, pageSize: 20, totalCount: 0 })).toBe(1);
    expect(pageCount({ items: [], page: 1, pageSize: 20, totalCount: 41 })).toBe(3);
  });
});

describe('ContractsService', () => {
  let service: ContractsService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAppEnvironment({ apiBaseUrl: '/api' }),
      ],
    });
    service = TestBed.inject(ContractsService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('searches by page, with the trimmed search text only when there is one', () => {
    service.search('', 1).subscribe();
    http.expectOne({ method: 'GET', url: '/api/contracts?page=1&pageSize=20' });

    service.search('  martin ', 2).subscribe();
    http.expectOne({ method: 'GET', url: '/api/contracts?page=2&pageSize=20&search=martin' });
  });

  it('reads, creates, updates and deletes contracts', () => {
    service.get('c-1').subscribe();
    http.expectOne({ method: 'GET', url: '/api/contracts/c-1' });

    service.create(input).subscribe();
    expect(http.expectOne({ method: 'POST', url: '/api/contracts' }).request.body).toEqual(input);

    service.update('c-1', input).subscribe();
    expect(http.expectOne({ method: 'PUT', url: '/api/contracts/c-1' }).request.body).toEqual(
      input,
    );

    service.delete('c-1').subscribe();
    http.expectOne({ method: 'DELETE', url: '/api/contracts/c-1' });
  });
});
