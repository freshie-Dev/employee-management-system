
export type EmploymentType =
  | 'PERMANENT'
  | 'CONTRACT'
  | 'INTERN';

export type EmploymentStatus =
  | 'ACTIVE'
  | 'ON_LEAVE'
  | 'TERMINATED';

export interface Employee {
  employeeId: number;
  userId: number;
  employeeCode: string;
  firstName: string;
  lastName: string;
  workEmail: string;
  personalEmail?: string | null;
  phoneNumber?: string | null;
  dateOfBirth?: string | null;
  hireDate: string;
  employmentType: EmploymentType;
  employmentStatus: EmploymentStatus;
}

export interface AvailableUser {
  userId: number;
  username: string;
}

export interface CreateEmployeeRequest {
  userId: number;
  employeeCode: string;
  firstName: string;
  lastName: string;
  workEmail: string;
  personalEmail: string | null;
  phoneNumber: string | null;
  dateOfBirth: string | null;
  hireDate: string;
  employmentType: EmploymentType;
}

export type UpdateEmployeeRequest = Pick<
  CreateEmployeeRequest,
  | 'firstName'
  | 'lastName'
  | 'workEmail'
  | 'personalEmail'
  | 'phoneNumber'
  | 'dateOfBirth'
  | 'employmentType'
>;

export interface EmployeeListParams {
  search?: string;
  page: number;
  pageSize: number;
}

export interface EmployeePage {
  total: number;
  page: number;
  pageSize: number;
  items: Employee[];
}
