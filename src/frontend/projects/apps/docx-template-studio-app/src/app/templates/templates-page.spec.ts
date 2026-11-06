import { Template } from '@docx-template-studio/shared/core';
import { vi } from 'vitest';
import { AppHarness } from '../../testing/app-harness';

const offer: Template = {
  id: 't-1',
  module: 'Contracts',
  name: 'Job offer',
  fileName: 'job-offer.docx',
  sizeBytes: 25_600,
  fieldKeys: ['employee.firstName', 'contract.startDate'],
  uploadedAtUtc: '2026-11-02T12:00:00Z',
};

const order: Template = {
  ...offer,
  id: 't-2',
  module: 'MissionOrders',
  name: 'Mission order',
  fileName: 'mission.docx',
  fieldKeys: [],
};

const docx = (name = 'agreement.docx') => new File(['PK'], name);

describe('Templates page', () => {
  let app: AppHarness;

  beforeEach(async () => {
    app = new AppHarness();
    app.signIn();
    await app.open('/templates');
  });

  afterEach(() => {
    app.http.verify();
    vi.restoreAllMocks();
  });

  async function answerList(templates: Template[], url = '/api/templates'): Promise<void> {
    app.http.expectOne(url).flush(templates);
    await app.settle();
  }

  it('lists the templates with their field keys', async () => {
    expect(app.text('[role="status"]')).toBe('Loading the templates…');

    await answerList([offer, order]);

    expect(app.texts('[data-testid="name"]')).toEqual(['Job offer', 'Mission order']);
    expect(app.texts('[data-testid="details"]')[0].replace(/\s+/g, ' ')).toBe(
      'Employment contracts · job-offer.docx · 25 KB · Added Nov 2, 2026',
    );
    expect(app.texts('ul[aria-label="Field keys"] code')).toEqual([
      'employee.firstName',
      'contract.startDate',
    ]);
    expect(app.text('[data-testid="templates"] li:last-child p.text-xs')).toBe('No field keys.');
  });

  it('filters by module', async () => {
    await answerList([offer, order]);

    await app.select('moduleFilter', 'MissionOrders');
    await answerList([order], '/api/templates?module=MissionOrders');

    expect(app.texts('[data-testid="name"]')).toEqual(['Mission order']);
  });

  it('says when there is no template and when the list fails', async () => {
    await answerList([]);
    expect(app.text('[data-testid="templates"] li')).toBe('No templates yet.');

    await app.select('moduleFilter', 'Contracts');
    app.http
      .expectOne('/api/templates?module=Contracts')
      .flush(null, { status: 500, statusText: 'Server Error' });
    await app.settle();
    expect(app.text('[role="alert"]')).toBe(
      'The templates could not be loaded. Please try again later.',
    );
  });

  describe('upload', () => {
    beforeEach(async () => await answerList([offer]));

    it('asks for the module and the file', async () => {
      await app.submit();

      expect(app.texts('dts-field-errors p')).toEqual(['Choose a module.', 'Choose a .docx file.']);
    });

    it('refuses other files and big files before uploading', async () => {
      await app.choose('file', new File(['%PDF'], 'offer.pdf'));
      expect(app.texts('dts-field-errors p')).toEqual([
        'Only Word .docx files can be used as templates.',
      ]);

      const big = docx('BIG.DOCX');
      Object.defineProperty(big, 'size', { value: 10 * 1024 * 1024 + 1 });
      await app.choose('file', big);
      expect(app.texts('dts-field-errors p')).toEqual(['The file is larger than 10 MB.']);

      await app.choose('file');
      expect(app.texts('dts-field-errors p')).toEqual(['Choose a .docx file.']);
    });

    it('uploads the template, shows its field keys and reloads the list', async () => {
      await app.select('module', 'TrainingAgreements');
      await app.type('name', 'Agreement');
      await app.choose('file', docx());

      await app.submit();
      const request = app.http.expectOne({ method: 'POST', url: '/api/templates' });
      const body = request.request.body as FormData;
      expect(body.get('module')).toBe('TrainingAgreements');
      expect(body.get('name')).toBe('Agreement');
      expect((body.get('file') as File).name).toBe('agreement.docx');
      const added = {
        ...offer,
        id: 't-3',
        module: 'TrainingAgreements',
        name: 'Agreement',
        fieldKeys: ['trainee.name', 'course.title'],
      };
      request.flush(added, { status: 201, statusText: 'Created' });
      await app.settle();
      await answerList([offer, added]);

      expect(app.text('[data-testid="uploaded"]')!.replace(/\s+/g, ' ')).toBe(
        '"Agreement" was added. Field keys found: trainee.name, course.title.',
      );
      expect(app.element.querySelector<HTMLSelectElement>('#module')!.value).toBe('');
      expect(app.element.querySelector<HTMLInputElement>('#file')!.value).toBe('');
      expect(app.texts('[data-testid="name"]')).toEqual(['Job offer', 'Agreement']);
    });

    it('tells when the template has no field keys', async () => {
      await app.select('module', 'MissionOrders');
      await app.choose('file', docx('mission.docx'));

      await app.submit();
      app.http.expectOne('/api/templates').flush(order, { status: 201, statusText: 'Created' });
      await app.settle();
      await answerList([offer, order]);

      expect(app.text('[data-testid="uploaded"]')!.replace(/\s+/g, ' ')).toBe(
        '"Mission order" was added. No field keys found: add tagged content controls in Word to fill this template.',
      );
    });

    it('shows a general message when the upload fails', async () => {
      await app.select('module', 'Contracts');
      await app.choose('file', docx());

      await app.submit();
      app.http.expectOne('/api/templates').flush(null, { status: 500, statusText: 'Server Error' });
      await app.settle();

      expect(app.text('form [role="alert"]')).toBe('Something went wrong. Please try again.');
    });

    it('shows why the API refused the file', async () => {
      await app.select('module', 'Contracts');
      await app.choose('file', docx('broken.docx'));

      await app.submit();
      app.http
        .expectOne('/api/templates')
        .flush(
          { errors: { file: ['This file is not a readable Word document.'] } },
          { status: 400, statusText: 'Bad Request' },
        );
      await app.settle();

      expect(app.texts('dts-field-errors p')).toEqual([
        'This file is not a readable Word document.',
      ]);
    });
  });

  describe('actions', () => {
    beforeEach(async () => await answerList([offer]));

    it('downloads the file with the access token', async () => {
      const createObjectURL = vi.fn(() => 'blob:offer');
      const revokeObjectURL = vi.fn();
      vi.stubGlobal('URL', Object.assign(URL, { createObjectURL, revokeObjectURL }));
      const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {});

      await app.click('[aria-label="Download Job offer"]');
      const request = app.http.expectOne('/api/templates/t-1/file');
      expect(request.request.headers.get('Authorization')).toBe('Bearer token-1');
      request.flush(new Blob(['PK']));
      await app.settle();

      expect(click).toHaveBeenCalledOnce();
      const link = click.mock.contexts[0] as HTMLAnchorElement;
      expect(link.download).toBe('job-offer.docx');
      expect(revokeObjectURL).toHaveBeenCalledWith('blob:offer');
      vi.unstubAllGlobals();
    });

    it('explains when the download fails', async () => {
      await app.click('[aria-label="Download Job offer"]');
      app.http
        .expectOne('/api/templates/t-1/file')
        .flush(new Blob(), { status: 404, statusText: 'Not Found' });
      await app.settle();

      expect(app.text('[role="alert"]')).toBe('Something went wrong. Please try again.');
    });

    it('deletes a template after confirmation', async () => {
      vi.spyOn(window, 'confirm').mockReturnValueOnce(false).mockReturnValue(true);

      await app.click('[aria-label="Delete Job offer"]');
      app.http.expectNone('/api/templates/t-1');

      await app.click('[aria-label="Delete Job offer"]');
      expect(app.button('[aria-label="Delete Job offer"]').disabled).toBe(true);
      app.http.expectOne({ method: 'DELETE', url: '/api/templates/t-1' }).flush(null);
      await app.settle();
      await answerList([]);

      expect(app.text('[data-testid="templates"] li')).toBe('No templates yet.');
    });

    it('keeps the template when the deletion fails', async () => {
      vi.spyOn(window, 'confirm').mockReturnValue(true);

      await app.click('[aria-label="Delete Job offer"]');
      app.http
        .expectOne('/api/templates/t-1')
        .flush(null, { status: 500, statusText: 'Server Error' });
      await app.settle();

      expect(app.text('[role="alert"]')).toBe('Something went wrong. Please try again.');
      expect(app.button('[aria-label="Delete Job offer"]').disabled).toBe(false);
    });
  });
});
