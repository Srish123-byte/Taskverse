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
  activeColleges: number;
  registeredStudents: number;
  assessmentsThisMonth: number;
  assessmentsPreviousMonth: number;
}

export interface PlatformHealth {
  uptimePercent: number;
  errorRatePercent: number;
  apiStatus: string;
}

export interface RecentActivity {
  action: string;
  entityType?: string;
  entityId?: string;
  performedBy: string;
  occurredAt: string;
  details?: string;
}

export interface CollegeScoreSummary {
  collegeId: string;
  collegeName: string;
  averageScore: number;
  studentsAssessed: number;
}

export interface UsageTrendPoint {
  date: string;
  assessments: number;
  studentsAssessed: number;
}

export interface SuperAdminDashboard {
  totals: SuperAdminTotals;
  pendingApprovals: College[];
  platformHealth: PlatformHealth;
  recentActivity: RecentActivity[];
  averageScoresByCollege: CollegeScoreSummary[];
  usageTrends: UsageTrendPoint[];
}
