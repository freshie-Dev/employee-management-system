import { inject, Injectable } from '@angular/core';
import { ApiClient } from '../../../core/api/api-client';
import { Shift, CreateShiftRequest, UpdateShiftRequest, ShiftPolicyRequest } from '../models/shift.model';

@Injectable({ providedIn: 'root' })
export class ShiftsApi {
  private readonly api = inject(ApiClient);

  list(includeInactive = true) {
    return this.api.get<Shift[]>('shifts', { includeInactive });
  }
  get(id: number) {
    return this.api.get<Shift>(`shifts/${id}`);
  }
  create(body: CreateShiftRequest) {
    return this.api.post<Shift, CreateShiftRequest>('shifts', body);
  }
  update(id: number, body: UpdateShiftRequest) {
    return this.api.put<Shift, UpdateShiftRequest>(`shifts/${id}`, body);
  }
  updatePolicy(id: number, body: ShiftPolicyRequest) {
    return this.api.put<Shift, ShiftPolicyRequest>(`shifts/${id}/policy`, body);
  }
  setStatus(id: number, isActive: boolean) {
    return this.api.patch<Shift, { isActive: boolean }>(`shifts/${id}/status`, { isActive });
  }
}
