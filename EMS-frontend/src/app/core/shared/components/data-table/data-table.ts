
import {
  Component,
  computed,
  contentChildren,
  input,
  output,
  signal
} from '@angular/core';

import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TagModule } from 'primeng/tag';

import {
  DataTableConfig,
  TableAction,
  TableActionEvent,
  TableColumn,
  DataTablePageEvent,

} from './data-table.model';

import {
  DataTableColumnDirective
} from './data-table-column.directive';
import { TooltipModule } from 'primeng/tooltip';

// const DEFAULT_CONFIG: Required<DataTableConfig> = {
const DEFAULT_CONFIG: DataTableConfig = {
  paginator: true,
  rows: 10,
  rowsPerPageOptions: [10, 20, 50],
  searchPlaceholder: 'Search records...',
  stripedRows: false,
  showGridlines: false,
  emptyMessage: 'No records found.',
};

@Component({
  selector: 'app-data-table',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    ButtonModule,
    InputTextModule,
    TagModule,
    TooltipModule
  ],
  templateUrl: './data-table.html',
  styleUrl: './data-table.scss'
})
export class DataTable<T extends object> {
  readonly data = input.required<T[]>();
  readonly columns = input.required<TableColumn<T>[]>();

  readonly config = input<DataTableConfig>({});
  readonly actions = input<TableAction<T>[]>([]);
  readonly loading = input(false);
  readonly embedded = input(false);

  // Used for server-side pagination.
  readonly totalRecords = input(0);
  readonly first = input(0);


  readonly actionClick =
    output<TableActionEvent<T>>();

  readonly pageChange =
    output<DataTablePageEvent>();

  readonly searchChange =
    output<string>();

  readonly templates = contentChildren(
    DataTableColumnDirective
  );

  readonly options = computed(() => ({
    ...DEFAULT_CONFIG,
    ...this.config()
  }));

  readonly searchTerm = signal('');

  readonly filteredData = computed(() => {
    const rows = this.data();
    const options = this.options();

    // Only filter locally when local search is enabled.
    if (options.searchMode !== 'local') {
      return rows;
    }

    const term = this.searchTerm().trim().toLowerCase();

    if (!term) {
      return rows;
    }

    return rows.filter(row =>
      this.columns().some(column => {
        const value = row[column.field];

        if (value == null) {
          return false;
        }

        return String(value)
          .toLowerCase()
          .includes(term);
      })
    );
  });

  readonly recordCount = computed(() =>
    this.options().searchMode == "server"
      ? this.totalRecords()
      : this.filteredData().length
  );

  readonly tableFirst = computed(() =>
    this.options().searchMode == "server" ? this.first() : 0
  );

  readonly visibleActions = (row: T) =>
    this.actions().filter(
      action => action.visible?.(row) ?? true
    );

  readonly actionColumnVisible = computed(
    () => this.actions().length > 0
  );


  columnTemplate(field: string) {
    return this.templates()
      .find(template => template.field() === field)
      ?.template ?? null;
  }

  onSearch(value: string): void {
    debugger;
    this.searchTerm.set(value);

    if (this.options().searchMode == "server") {
      this.searchChange.emit(value.trim());
    }
  }

  onPageChange(event: {
    first?: number;
    rows?: number;
  }): void {
    if (this.options().searchMode == 'local') {
      return;
    }

    const pageSize = event.rows ?? this.options().rows ?? 10;
    const first = event.first ?? 0;

    this.pageChange.emit({
      page: Math.floor(first / pageSize) + 1,
      pageSize
    });
  }

  handleAction(action: TableAction<T>, row: T): void {
    if (action.disabled?.(row) || action.loading?.(row)) {
      return;
    }

    this.actionClick.emit({
      action: action.id,
      row
    });
  }

  // Standard status-cell rendering.
  statusLabel(
    value: unknown,
    column: TableColumn<T>
  ): string {
    if (typeof value === 'boolean') {
      return value
        ? column.trueLabel ?? 'Active'
        : column.falseLabel ?? 'Inactive';
    }

    if (typeof value === 'string') {
      return value
        .replaceAll('_', ' ')
        .toLowerCase()
        .replace(/\b\w/g, char => char.toUpperCase());
    }

    return String(value ?? '—');
  }

  statusSeverity(
    value: unknown
  ): 'success' | 'warn' | 'danger' | 'secondary' {
    if (value === true || value === 'ACTIVE') {
      return 'success';
    }

    if (value === 'ON_LEAVE') {
      return 'warn';
    }

    if (value === 'TERMINATED') {
      return 'danger';
    }

    return 'secondary';
  }
}
