export interface CollegeActionRequest {
  reason?: string;
}

export interface CollegeSearchRequest {
  query?: string;
  status: string;
}

export interface CollegeSearchResult {
  collegeId: string;
  name: string;
  city?: string;
  state?: string;
  adminName?: string;
  adminEmail?: string;
  totalUsers: number;
  status: string;
}

export interface UserActionRequest {
  reason?: string;
}

export interface PendingUser {
  userId: string;
  fullName: string;
  email: string;
  role: string;
  status: string;
  createdAt: string;
  institutionName?: string;
}

export interface UserSearchRequest {
  status?: string;
  role?: string;
  searchTerm?: string;
  pageNumber: number;
  pageSize: number;
}

export interface PagedUsersResult {
  items: PendingUser[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export interface BulkStudentUploadRow {
  fullName: string;
  email: string;
  phone: string;
  enrollmentNumber?: string;
  collegeId?: string;
  collegeName?: string;
  classId?: string;
  batchId?: string;
  className?: string;
}

export interface BulkStudentUploadRequest {
  rows: BulkStudentUploadRow[];
}

export interface BulkStudentUploadCreatedUser {
  fullName: string;
  email: string;
}

export interface BulkStudentUploadRowIssue {
  rowNumber: number;
  email: string;
  message: string;
}

export interface BulkStudentUploadResult {
  createdCount: number;
  duplicateCount: number;
  invalidCount: number;
  createdUsers: BulkStudentUploadCreatedUser[];
  duplicateRows: BulkStudentUploadRowIssue[];
  invalidRows: BulkStudentUploadRowIssue[];
}

export interface College {
  collegeId: string;
  name: string;
  city?: string;
  state?: string;
  status: string;
  approvalStatus: string;
  isActive: boolean;
  requestedAt: string;
  requestedBy?: string;
  approvedAt?: string;
  approvedBy?: string;
  notes?: string;
}

export interface SuperAdminTotals {
  pendingApprovals: number;
  activeColleges: number;
  registeredStudents: number;
  assessmentsThisMonth: number;
}

export interface SuperAdminDashboard {
  totals: SuperAdminTotals;
}
