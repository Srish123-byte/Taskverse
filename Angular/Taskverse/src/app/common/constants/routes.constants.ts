export const RouteAddress = {
  Base:           '/',
  Login:          'login',
  Logout:         'logout',
  SessionTimeout: 'session-timeout',
  RoleDirector:   'role-director',
  ApprovalStatus: 'approval-status',
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
    QuestionsManagement:'college-admin/questions-management',
    AssessmentBuilder:  'college-admin/assessment-builder',
    Reports:            'college-admin/reports',
    HelpCenter:         'college-admin/help-center',
    Settings:           'college-admin/settings'
  },

  Trainer: {
    Root:       'trainer',
    Dashboard:  'trainer/dashboard',
    Courses:    'trainer/courses',
    Students:   'trainer/students',
    Manage:     'trainer/manage',
    HelpCenter: 'trainer/help-center'
  },

  Student: {
    Root:          'student',
    Dashboard:     'student/dashboard',
    MyAssessments: 'student/my-assessments',
    Results:       'student/results',
    HelpCenter:    'student/help-center'
  }
};
