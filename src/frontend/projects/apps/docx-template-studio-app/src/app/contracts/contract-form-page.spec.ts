import { Contract } from '@docx-template-studio/shared/core';
import { AppHarness } from '../../testing/app-harness';

const contract: Contract = {
  id: 'c-2',
  employeeFirstName: 'Maya',
  employeeLastName: 'Roux',
  position: 'Sales assistant',
  type: 'FixedTerm',
  startDate: '2026-01-05',
  endDate: '2026-06-30',
  grossMonthlySalary: 2150.5,
  createdAtUtc: '2026-01-01T09:00:00Z',
  updatedAtUtc: '2026-01-01T09:00:00Z',
};

describe('Contract form page', () => {
  let app: AppHarness;

  beforeEach(() => {
    app = new AppHarness();
    app.signIn();
  });

  afterEach(() => app.http.verify());

  function value(id: string): string {
    return app.element.querySelector<HTMLInputElement | HTMLSelectElement>(`#${id}`)!.value;
  }

  async function backToList(): Promise<void> {
    await app.settle();
    app.http.expectOne('/api/contracts?page=1&pageSize=20').flush({
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
    });
    await app.settle();
    expect(app.router.url).toBe('/contracts');
  }

  describe('new contract', () => {
    beforeEach(async () => await app.open('/contracts/new'));

    it('asks for the required fields before calling the API', async () => {
      await app.submit();

      expect(app.text('h1')).toBe('New contract');
      expect(app.texts('dts-field-errors p')).toEqual([
        'Enter the first name.',
        'Enter the last name.',
        'Enter the position.',
        'Enter the start date.',
        'Enter a gross monthly salary of 0 or more.',
      ]);
    });

    it('refuses a negative salary before calling the API', async () => {
      await app.type('grossMonthlySalary', '-1');

      expect(app.texts('dts-field-errors p')).toEqual([
        'Enter a gross monthly salary of 0 or more.',
      ]);
    });

    it('creates a permanent contract without an end date', async () => {
      await app.type('employeeFirstName', 'Leo');
      await app.type('employeeLastName', 'Martin');
      await app.type('position', 'Accountant');
      await app.type('startDate', '2026-01-05');
      await app.type('grossMonthlySalary', '3200');

      await app.submit();
      const request = app.http.expectOne({ method: 'POST', url: '/api/contracts' });
      expect(request.request.body).toEqual({
        employeeFirstName: 'Leo',
        employeeLastName: 'Martin',
        position: 'Accountant',
        type: 'Permanent',
        startDate: '2026-01-05',
        endDate: null,
        grossMonthlySalary: 3200,
      });
      request.flush({ ...contract, id: 'c-9' }, { status: 201, statusText: 'Created' });

      await backToList();
    });

    it('shows a general message when the API fails', async () => {
      await app.type('employeeFirstName', 'Leo');
      await app.type('employeeLastName', 'Martin');
      await app.type('position', 'Accountant');
      await app.type('startDate', '2026-01-05');
      await app.type('grossMonthlySalary', '3200');

      await app.submit();
      app.http.expectOne('/api/contracts').flush(null, { status: 500, statusText: 'Server Error' });
      await app.settle();

      expect(app.text('[role="alert"]')).toBe('Something went wrong. Please try again.');
    });

    it('shows the API messages next to each field', async () => {
      await app.type('employeeFirstName', 'Leo');
      await app.type('employeeLastName', 'Martin');
      await app.type('position', 'Intern');
      await app.select('type', 'Internship');
      await app.type('startDate', '2026-01-05');
      await app.type('grossMonthlySalary', '900');

      await app.submit();
      const request = app.http.expectOne('/api/contracts');
      expect(request.request.body).toMatchObject({ type: 'Internship', grossMonthlySalary: 900 });
      request.flush(
        {
          errors: {
            position: ['Use at most 150 characters.'],
            endDate: ['This type of contract needs an end date.'],
          },
        },
        { status: 400, statusText: 'Bad Request' },
      );
      await app.settle();

      expect(app.texts('dts-field-errors p')).toEqual([
        'Use at most 150 characters.',
        'This type of contract needs an end date.',
      ]);
    });
  });

  describe('existing contract', () => {
    it('fills the form and saves the changes', async () => {
      await app.open('/contracts/c-2');
      expect(app.text('[role="status"]')).toBe('Loading the contract…');
      app.http.expectOne('/api/contracts/c-2').flush(contract);
      await app.settle();

      expect(app.text('h1')).toBe('Edit contract');
      expect(value('employeeFirstName')).toBe('Maya');
      expect(value('type')).toBe('FixedTerm');
      expect(value('endDate')).toBe('2026-06-30');
      expect(value('grossMonthlySalary')).toBe('2150.5');

      await app.type('position', 'Store manager');
      await app.submit();
      const request = app.http.expectOne({ method: 'PUT', url: '/api/contracts/c-2' });
      expect(request.request.body).toMatchObject({
        position: 'Store manager',
        endDate: '2026-06-30',
      });
      request.flush({ ...contract, position: 'Store manager' });

      await backToList();
    });

    it('leaves the end date empty for a permanent contract', async () => {
      await app.open('/contracts/c-1');
      app.http.expectOne('/api/contracts/c-1').flush({ ...contract, endDate: null });
      await app.settle();

      expect(value('endDate')).toBe('');
    });

    it('says when the contract does not exist', async () => {
      await app.open('/contracts/c-404');
      app.http
        .expectOne('/api/contracts/c-404')
        .flush(null, { status: 404, statusText: 'Not Found' });
      await app.settle();

      expect(app.text('.alert-info')).toBe(
        'This contract does not exist. It may have been deleted.',
      );
    });

    it('explains when the contract cannot be loaded', async () => {
      await app.open('/contracts/c-2');
      app.http
        .expectOne('/api/contracts/c-2')
        .flush(null, { status: 500, statusText: 'Server Error' });
      await app.settle();

      expect(app.text('[role="alert"]')).toBe(
        'The contract could not be loaded. Please try again later.',
      );
    });
  });
});
