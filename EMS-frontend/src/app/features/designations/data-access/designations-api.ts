
import { inject, Injectable } from '@angular/core';
import { ApiClient } from '../../../core/api/api-client';

import {
  Designation,
  DesignationRequest
} from '../models/designation.model';

@Injectable({
  providedIn: 'root'
})
export class DesignationsApi {
  private readonly api = inject(ApiClient);

  list() {
    return this.api.get<Designation[]>('designations');
  }

  get(id: number) {
    return this.api.get<Designation>(`designations/${id}`);
  }

  create(data: DesignationRequest) {
    return this.api.post<Designation, DesignationRequest>(
      'designations',
      data
    );
  }

  update(id: number, data: DesignationRequest) {
    return this.api.put<Designation, DesignationRequest>(
      `designations/${id}`,
      data
    );
  }

  setStatus(id: number, isActive: boolean) {
    return this.api.patch<void, { isActive: boolean }>(
      `designations/${id}/status`,
      { isActive }
    );
  }
}
