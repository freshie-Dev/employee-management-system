type QueryValue = string | number | boolean;

export type ApiQuery = Record<
    string,
    QueryValue | null | undefined
>;
export interface ApiError {
  status: number;
  message: string;
  validationErrors?: Record<string, string[]>;
}