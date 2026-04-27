export enum MixpanelEvents {
  // Auth
  LOGIN_VIEWED        = 'login_viewed',
  LOGIN_SUBMITTED     = 'login_submitted',
  LOGIN_SUCCEEDED     = 'login_succeeded',
  LOGIN_FAILED        = 'login_failed',
  LOGOUT_CLICKED      = 'logout_clicked',

  // Navigation
  PAGE_VIEWED         = 'page_viewed',
  DASHBOARD_VIEWED    = 'dashboard_viewed',

  // Super Admin
  COLLEGE_CREATED     = 'college_created',
  COLLEGE_UPDATED     = 'college_updated',
  COLLEGE_DELETED     = 'college_deleted',
  TRAINER_INVITED     = 'trainer_invited',
  USER_DEACTIVATED    = 'user_deactivated',

  // College Admin
  COURSE_CREATED      = 'course_created',
  COURSE_UPDATED      = 'course_updated',
  COURSE_DELETED      = 'course_deleted',
  STUDENT_ENROLLED    = 'student_enrolled',

  // Trainer
  COURSE_PUBLISHED    = 'course_published',
  TASK_CREATED        = 'task_created',
  TASK_UPDATED        = 'task_updated',

  // Student
  COURSE_STARTED      = 'course_started',
  TASK_SUBMITTED      = 'task_submitted',
  TASK_VIEWED         = 'task_viewed'
}
