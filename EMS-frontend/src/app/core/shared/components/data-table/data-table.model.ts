
export type TableColumnType =
    | 'text'
    | 'number'
    | 'date'
    | 'status'
    | 'custom';

export interface TableColumn<T> {
    field: Extract<keyof T, string>;
    header: string;
    type?: TableColumnType;
    sortable?: boolean;
    width?: string;
    align?: 'left' | 'center' | 'right';

    // Used by the built-in status renderer.
    // Example: isActive -> Active / Inactive
    trueLabel?: string;
    falseLabel?: string;
}

export interface DataTableConfig {
    paginator?: boolean;
    rows?: number;
    rowsPerPageOptions?: number[];

    searchPlaceholder?: string;

    stripedRows?: boolean;
    showGridlines?: boolean;
    emptyMessage?: string;

    // True when the parent API provides a single page.
    //   serverSide?: boolean;
    //   serverSearch?: boolean;
    searchMode?: "local" | "server";
}

export interface TableAction<T> {
    id: string;
    label: string;
    icon: string;
    severity?:
    | 'primary'
    | 'secondary'
    | 'success'
    | 'info'
    | 'warn'
    | 'danger';

    visible?: (row: T) => boolean;
    disabled?: (row: T) => boolean;
    loading?: (row: T) => boolean;
}

export interface TableActionEvent<T> {
    action: string;
    row: T;
}

export interface DataTablePageEvent {
    page: number;
    pageSize: number;
}