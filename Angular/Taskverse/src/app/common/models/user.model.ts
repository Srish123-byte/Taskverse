import { RoleType } from '../enums/role-type.enum';

export interface User {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  role: RoleType;
  isActive: boolean;
}
