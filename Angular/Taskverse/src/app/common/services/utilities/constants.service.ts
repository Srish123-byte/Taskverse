import { Injectable } from '@angular/core';

@Injectable()
export class ConstantsService {
  public static errorCodes: number[] = [401, 403, 404, 500];
}
