import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { APP_ENVIRONMENT } from '@docx-template-studio/shared/environment';
import { Observable } from 'rxjs';
import { Contract, ContractInput, PagedResponse } from './contracts.models';

export const CONTRACTS_PAGE_SIZE = 20;

@Injectable({ providedIn: 'root' })
export class ContractsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(APP_ENVIRONMENT).apiBaseUrl}/contracts`;

  search(search: string, page: number): Observable<PagedResponse<Contract>> {
    let params = new HttpParams().set('page', page).set('pageSize', CONTRACTS_PAGE_SIZE);
    if (search.trim()) {
      params = params.set('search', search.trim());
    }
    return this.http.get<PagedResponse<Contract>>(this.baseUrl, { params });
  }

  get(id: string): Observable<Contract> {
    return this.http.get<Contract>(`${this.baseUrl}/${id}`);
  }

  create(input: ContractInput): Observable<Contract> {
    return this.http.post<Contract>(this.baseUrl, input);
  }

  update(id: string, input: ContractInput): Observable<Contract> {
    return this.http.put<Contract>(`${this.baseUrl}/${id}`, input);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
