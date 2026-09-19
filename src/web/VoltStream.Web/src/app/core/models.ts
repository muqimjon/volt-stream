export type PrintPolicy = 'Auto' | 'Ask' | 'Deny';

export type PrintJobStatus =
  | 'Pending'
  | 'Printing'
  | 'Completed'
  | 'Failed'
  | 'Rejected'
  | 'Cancelled';

export interface LoginResult {
  id: number;
  username: string;
  token: string;
  role: string;
  branchId: number | null;
  branchName: string | null;
  permissions: string[];
  printPolicy: PrintPolicy;
}

export interface Branch {
  id: number;
  name: string;
  isActive: boolean;
}

export interface Category {
  id: number;
  name: string;
}

export interface Product {
  id: number;
  name: string;
  unit: string;
  categoryId: number;
  category?: Category;
  isWeightTracked: boolean;
  weightPerUnit?: number | null;
}

export interface WarehouseItem {
  id: number;
  lengthPerRoll: number;
  rollCount: number;
  totalLength: number;
  unitPrice: number;
  discountRate: number;
  branchId?: number | null;
}

export interface Customer {
  id: number;
  name: string;
  phone?: string | null;
}

export interface Currency {
  id: number;
  name: string;
  code: string;
  isDefault: boolean;
  exchangeRate: number;
}

export interface SaleItemRequest {
  productId: number;
  rollCount: number;
  lengthPerRoll: number;
  totalLength: number;
  unitPrice: number;
  discountRate: number;
  discountAmount: number;
  totalAmount: number;
  finalAmount: number;
}

export interface SaleRequest {
  date: string;
  customerId?: number | null;
  currencyId: number;
  rollCount: number;
  length: number;
  amount: number;
  isDiscountApplied: boolean;
  discount: number;
  description?: string | null;
  branchId?: number | null;
  items: SaleItemRequest[];
}

export interface SupplyRequest {
  date: string;
  categoryId: number;
  categoryName: string;
  productId: number;
  productName: string;
  rollCount: number;
  unit: string;
  lengthPerRoll: number;
  totalLength: number;
  unitPrice?: number | null;
  discountRate?: number | null;
  branchId?: number | null;
}

export interface PrintJob {
  id: number;
  saleId: number;
  status: PrintJobStatus;
  error?: string | null;
  createdAt?: string;
}

export interface RollGroup {
  length: number;
  count: number;
}

export interface SaleReceiptItem {
  category: string;
  product: string;
  lengthPerRoll: number;
  rollCount: number;
  totalLength: number;
  composition: string;
  rolls?: RollGroup[];
  unit: string;
  weight: number;
  weightPerUnit?: number | null;
  unitPrice?: number | null;
  totalAmount?: number | null;
}

export interface SaleReceipt {
  saleId: number;
  date: string;
  branch: string;
  customer?: string | null;
  operator?: string | null;
  items: SaleReceiptItem[];
  totalRolls: number;
  totalLength: number;
  totalWeight: number;
  hasPrices: boolean;
  amount?: number | null;
  discount?: number | null;
}

export interface SaleListItem {
  id: number;
  date: string;
  rollCount: number;
  length: number;
  amount: number;
  customer?: { name: string } | null;
  items?: { productId: number; totalLength: number; rollCount: number; weight?: number }[];
  branchId?: number | null;
  printJob?: PrintJob | null;
}

export interface SupplyListItem {
  id: number;
  date: string;
  rollCount: number;
  lengthPerRoll: number;
  totalLength: number;
  product?: { name: string; category?: { name: string } | null } | null;
  branchId?: number | null;
}

export interface BranchStats {
  salesCount: number;
  soldRolls: number;
  soldLength: number;
  soldWeight: number;
  suppliesCount: number;
  receivedRolls: number;
  receivedLength: number;
  receivedWeight: number;
}

export interface AuditLog {
  id: number;
  timestamp: string;
  username: string;
  action: string;
  entityName?: string | null;
  entityId?: number | null;
  details?: string | null;
  success: boolean;
  error?: string | null;
}

export interface FilterRequest {
  filters?: Record<string, string[]>;
  search?: string | null;
  page: number;
  pageSize: number;
  sortBy?: string | null;
  descending?: boolean;
  timeZone?: number;
}

export interface Paging {
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface Paged<T> {
  items: T[];
  paging: Paging | null;
}
