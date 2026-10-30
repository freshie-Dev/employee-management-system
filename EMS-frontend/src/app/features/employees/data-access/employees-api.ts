
import { inject, Injectable } from '@angular/core';

import { ApiClient } from '../../../core/api/api-client';

import {
    Employee,
    EmployeePage,
    EmployeeListParams,
    CreateEmployeeRequest,
    UpdateEmployeeRequest,
    EmploymentStatus,
    AvailableUser,
} from '../models/employee.model';

@Injectable({
    providedIn: 'root'
})
export class EmployeesApi {
    private readonly api = inject(ApiClient);

    list(params: EmployeeListParams) {
        const query = new URLSearchParams({
            page: String(params.page),
            pageSize: String(params.pageSize)
        });

        if (params.search?.trim()) {
            query.set('search', params.search.trim());
        }

        return this.api.get<EmployeePage>(
            `employees?${query.toString()}`
        );
    }

    get(id: number) {
        return this.api.get<Employee>(`employees/${id}`);
    }

    create(data: CreateEmployeeRequest) {
        return this.api.post<Employee, CreateEmployeeRequest>(
            'employees',
            data
        );
    }

    update(id: number, data: UpdateEmployeeRequest) {
        return this.api.put<Employee, UpdateEmployeeRequest>(
            `employees/${id}`,
            data
        );
    }

    setStatus(id: number, status: EmploymentStatus) {
        return this.api.patch<void, { status: EmploymentStatus }>(
            `employees/${id}/status`,
            { status }
        );
    }

    getAvailableUsers() {
        return this.api.get<AvailableUser[]>(
            'employees/available-users'
        );
    }
}
