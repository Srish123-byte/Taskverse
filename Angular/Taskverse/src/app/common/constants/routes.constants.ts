export const RouteAddress = {
  Base:           '/',
  Login:          'login',
  Logout:         'logout',
  SessionTimeout: 'session-timeout',
  RoleDirector:   'role-director',
  ApprovalStatus: 'approval-status',
  ChangeTemporaryPassword: 'change-temporary-password',
  Error:          'error',

  SuperAdmin: {
    Root:       'super-admin',
    Dashboard:  'super-admin/dashboard',
    Colleges:   'super-admin/colleges',
    Users:      'super-admin/users',
    Analytics:  'super-admin/analytics',
    Assessments:'super-admin/assessments',
    Settings:   'super-admin/settings'
  },

  CollegeAdmin: {
    Root:               'college-admin',
    Dashboard:          'college-admin/dashboard',
    Approvals:          'college-admin/approvals',
    Users:              'college-admin/users',
    ClassesManagement:  'college-admin/classes-management',
    AttendanceTracker:  'college-admin/classes-management/attendance-tracker',
    QuestionsManagement:'college-admin/questions-management',
    AddQuestion:        'college-admin/questions-management/new',
    AssessmentsManagement: 'college-admin/assessments-management',
    NewAssessment:      'college-admin/assessments-management/new-assessment',
    EditAssessment:     'college-admin/assessments-management/edit-assessment',
    Reports:            'college-admin/reports',
    HelpCenter:         'college-admin/help-center',
    Settings:           'college-admin/settings'
  },

  Trainer: {
    Root:                'trainer',
    Dashboard:           'trainer/dashboard',
    Courses:             'trainer/courses',
    Students:            'trainer/students',
    QuestionsManagement: 'trainer/questions-management',
    AddQuestion:         'trainer/questions-management/new',
    AssessmentsManagement: 'trainer/assessments-management',
    NewAssessment:       'trainer/assessments-management/new-assessment',
    EditAssessment:      'trainer/assessments-management/edit-assessment',
    Manage:              'trainer/manage',
    HelpCenter:          'trainer/help-center'
  },

  Student: {
    Root:             'student',
    Dashboard:        'student/dashboard',
    MyAssessments:    'student/my-assessments',
    AssessmentRunner: 'student/my-assessments/attempts',
    Results:          'student/results',
    AttemptResults:   'student/results/attempts',
    HelpCenter:       'student/help-center'
  }
};
