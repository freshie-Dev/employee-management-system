
export interface Designation {
  designationId: number;
  title: string;
  code: string;
  description: string | null;
  isActive: boolean;
}

export interface DesignationRequest {
  title: string;
  code: string;
  description: string | null;
}
