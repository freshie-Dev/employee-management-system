export interface Department {
  departmentId: number;
  name: string;
  code: string;
  parentDepartmentId: number | null;
  isActive: boolean;
}

export interface DepartmentRequest {
  name: string;
  code: string;
  parentDepartmentId: number | null;
}
