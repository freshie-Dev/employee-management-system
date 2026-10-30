
import {
  Directive,
  TemplateRef,
  inject,
  input
} from '@angular/core';

@Directive({
  selector: 'ng-template[appTableColumn]'
})
export class DataTableColumnDirective {
  readonly field = input.required<string>({
    alias: 'appTableColumn'
  });

  readonly template = inject(TemplateRef<{
    $implicit: unknown;
  }>);
}
