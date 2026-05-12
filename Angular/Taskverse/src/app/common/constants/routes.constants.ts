export const RouteAddress = {
  Base:           '/',
  Login:          'login',
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
    Root:      'college-admin',
    Dashboard: 'college-admin/dashboard',
    Courses:   'college-admin/courses',
    Trainers:  'college-admin/trainers',
    Students:  'college-admin/students',
    Manage:    'college-admin/manage'
  },

  Trainer: {
    Root:      'trainer',
    Dashboard: 'trainer/dashboard',
    Courses:   'trainer/courses',
    Students:  'trainer/students',
    Manage:    'trainer/manage'
  },

  Student: {
    Root:      'student',
    Dashboard: 'student/dashboard',
    Courses:   'student/courses',
    Tasks:     'student/tasks',
    Manage:    'student/manage'
  }
};
