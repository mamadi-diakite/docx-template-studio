import { Contract, PagedResponse } from '@docx-template-studio/shared/core';
import { vi } from 'vitest';
import { AppHarness } from '../../testing/app-harness';

const permanent: Contract = {
  id: 'c-1',
  employeeFirstName: 'Leo',
  employeeLastName: 'Martin',
  position: 'Accountant',
  type: 'Permanent',
  startDate: '2026-01-05',
  endDate: null,
  grossMonthlySalary: 3200,
  createdAtUtc: '2026-01-01T09:00:00Z',
  updatedAtUtc: '2026-01-01T09:00:00Z',
};

const fixedTerm: Contract = {
  ...permanent,
  id: 'c-2',
  employeeFirstName: 'Maya',
  employeeLastName: 'Roux',
  position: 'Sales assistant',
  type: 'FixedTerm',
  endDate: '2026-06-30',
  grossMonthlySalary: 2150.5,
};

function page(items: Contract[], number = 1, totalCount = 45): PagedResponse<Contract> {
  return { items, page: number, pageSize: 20, totalCount };
}

describe('Contracts page', () => {
  let app: AppHarness;

  beforeEach(async () => {
    app = new AppHarness();
    app.signIn();
    await app.open('/contracts');
  });

  afterEach(() => {
    app.http.verify();
    vi.restoreAllMocks();
  });

  async function answer(url: string, response: PagedResponse<Contract>): Promise<void> {
    app.http.expectOne(url).flush(response);
    await app.settle();
  }

  function rows(): string[][] {
    return [...app.element.querySelectorAll('tbody tr')].map((row) =>
      [...row.querySelectorAll('td')]
        .slice(0, 5)
        .map((cell) => cell.textContent!.replace(/\s+/g, ' ').trim()),
    );
  }

  it('lists the first page of contracts', async () => {
    expect(app.text('[role="status"]')).toBe('Loading the contracts…');

    await answer('/api/contracts?page=1&pageSize=20', page([permanent, fixedTerm]));

    expect(rows()).toEqual([
      ['Leo Martin', 'Accountant', 'Permanent', 'Jan 5, 2026', '€3,200.00'],
      ['Maya Roux', 'Sales assistant', 'Fixed-term', 'Jan 5, 2026 – Jun 30, 2026', '€2,150.50'],
    ]);
    expect(app.element.querySelector('tbody a')?.getAttribute('href')).toBe('/contracts/c-1');
    expect(app.text('[data-testid="page"]')).toBe('Page 1 of 3 · 45 contracts');
    expect(app.button('nav[aria-label="Pages"] button:first-child').disabled).toBe(true);
  });

  it('moves between pages', async () => {
    await answer('/api/contracts?page=1&pageSize=20', page([permanent]));

    await app.click('nav[aria-label="Pages"] button:last-child');
    await answer('/api/contracts?page=2&pageSize=20', page([fixedTerm], 2));
    expect(app.text('[data-testid="page"]')).toBe('Page 2 of 3 · 45 contracts');

    await app.click('nav[aria-label="Pages"] button:first-child');
    await answer('/api/contracts?page=1&pageSize=20', page([permanent]));
    expect(app.text('[data-testid="page"]')).toBe('Page 1 of 3 · 45 contracts');
  });

  it('disables Next on the last page', async () => {
    await answer('/api/contracts?page=1&pageSize=20', page([permanent], 1, 1));

    expect(app.button('nav[aria-label="Pages"] button:last-child').disabled).toBe(true);
  });

  it('searches from the first page', async () => {
    await answer('/api/contracts?page=1&pageSize=20', page([permanent]));

    await app.type('search', 'roux');
    await app.submit();
    await answer('/api/contracts?page=1&pageSize=20&search=roux', page([], 1, 0));

    expect(app.text('[data-testid="empty"]')).toBe('No contracts found.');
  });

  it('explains when the contracts cannot be loaded', async () => {
    app.http
      .expectOne('/api/contracts?page=1&pageSize=20')
      .flush(null, { status: 500, statusText: 'Server Error' });
    await app.settle();

    expect(app.text('[role="alert"]')).toBe(
      'The contracts could not be loaded. Please try again later.',
    );
  });

  it('deletes a contract after confirmation and reloads the page', async () => {
    await answer('/api/contracts?page=1&pageSize=20', page([permanent, fixedTerm]));
    const confirm = vi.spyOn(window, 'confirm').mockReturnValueOnce(false).mockReturnValue(true);

    await app.click('[aria-label="Delete the contract of Leo Martin"]');
    expect(confirm).toHaveBeenCalledWith('Delete the contract of Leo Martin?');
    app.http.expectNone('/api/contracts/c-1');

    await app.click('[aria-label="Delete the contract of Leo Martin"]');
    expect(app.button('[aria-label="Delete the contract of Maya Roux"]').disabled).toBe(true);
    app.http.expectOne({ method: 'DELETE', url: '/api/contracts/c-1' }).flush(null);
    await app.settle();
    await answer('/api/contracts?page=1&pageSize=20', page([fixedTerm], 1, 44));

    expect(rows().map((row) => row[0])).toEqual(['Maya Roux']);
  });

  it('keeps the contract when the deletion fails', async () => {
    await answer('/api/contracts?page=1&pageSize=20', page([permanent]));
    vi.spyOn(window, 'confirm').mockReturnValue(true);

    await app.click('[aria-label="Delete the contract of Leo Martin"]');
    app.http.expectOne('/api/contracts/c-1').flush(null, { status: 404, statusText: 'Not Found' });
    await app.settle();

    expect(app.text('[role="alert"]')).toBe('Something went wrong. Please try again.');
    expect(app.button('[aria-label="Delete the contract of Leo Martin"]').disabled).toBe(false);
  });
});
