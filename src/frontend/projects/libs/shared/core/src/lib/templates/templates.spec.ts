import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideAppEnvironment } from '@docx-template-studio/shared/environment';
import { moduleLabel } from './templates.models';
import { TemplatesService } from './templates.service';

describe('moduleLabel', () => {
  it('labels the modules', () => {
    expect(moduleLabel('MissionOrders')).toBe('Mission orders');
    expect(moduleLabel('Other')).toBe('Other');
  });
});

describe('TemplatesService', () => {
  let service: TemplatesService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAppEnvironment({ apiBaseUrl: '/api' }),
      ],
    });
    service = TestBed.inject(TemplatesService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('lists all templates or the ones of a module', () => {
    service.list('').subscribe();
    http.expectOne({ method: 'GET', url: '/api/templates' });

    service.list('Contracts').subscribe();
    http.expectOne({ method: 'GET', url: '/api/templates?module=Contracts' });
  });

  it('uploads the module, the trimmed name when given, and the file', () => {
    const file = new File(['docx'], 'offer.docx');

    service.upload({ module: 'Contracts', name: ' Offer ', file }).subscribe();
    const named = http.expectOne({ method: 'POST', url: '/api/templates' }).request
      .body as FormData;
    expect(named.get('module')).toBe('Contracts');
    expect(named.get('name')).toBe('Offer');
    expect((named.get('file') as File).name).toBe('offer.docx');

    service.upload({ module: 'Contracts', name: '  ', file }).subscribe();
    const unnamed = http.expectOne('/api/templates').request.body as FormData;
    expect(unnamed.has('name')).toBe(false);
  });

  it('downloads the file as a blob and deletes a template', () => {
    service.download('t-1').subscribe();
    const download = http.expectOne({ method: 'GET', url: '/api/templates/t-1/file' });
    expect(download.request.responseType).toBe('blob');

    service.delete('t-1').subscribe();
    http.expectOne({ method: 'DELETE', url: '/api/templates/t-1' });
  });
});
