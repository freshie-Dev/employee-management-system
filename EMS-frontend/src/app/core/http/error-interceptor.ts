
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { ApiError } from '../models/api.models';


export const errorInterceptor: HttpInterceptorFn =
  (request, next) => {

    return next(request).pipe(
      catchError((error: unknown) => {

        if (!(error instanceof HttpErrorResponse)) {
          return throwError(() => error);
        }

        const body = error.error;

        const message =
          typeof body === 'string' && body.trim()
            ? body
            : typeof body?.message === 'string'
              ? body.message
              : typeof body?.detail === 'string'
                ? body.detail
                : getDefaultMessage(error.status);

        const apiError: ApiError = {
          status: error.status,
          message,
          validationErrors: body?.errors
        };

        return throwError(() => apiError);
      })
    );
  };

function getDefaultMessage(status: number): string {
  switch (status) {
    case 0:
      return 'Unable to connect to the server.';
    case 400:
      return 'Invalid request.';
    case 401:
      return 'Authentication required.';
    case 403:
      return 'You do not have permission.';
    case 404:
      return 'Resource not found.';
    case 409:
      return 'The request conflicts with existing data.';
    default:
      return 'Something went wrong.';
  }
}
