import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { APP_ENVIRONMENT } from '@docx-template-studio/shared/environment';
import { Observable } from 'rxjs';
import { Template, TemplateUpload } from './templates.models';

@Injectable({ providedIn: 'root' })
export class TemplatesService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(APP_ENVIRONMENT).apiBaseUrl}/templates`;

  /** All templates, or the ones of a module. */
  list(module: string): Observable<Template[]> {
    const params = module ? new HttpParams().set('module', module) : undefined;
    return this.http.get<Template[]>(this.baseUrl, { params });
  }

  upload({ module, name, file }: TemplateUpload): Observable<Template> {
    const body = new FormData();
    body.append('module', module);
    if (name.trim()) {
      body.append('name', name.trim());
    }
    body.append('file', file, file.name);
    return this.http.post<Template>(this.baseUrl, body);
  }

  /** The file goes through the API with the access token, so it cannot be a plain link. */
  download(id: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/${id}/file`, { responseType: 'blob' });
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
