import { LowerCasePipe } from '@angular/common';
import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'toLowerCaseText',
  standalone: false
})
export class ToLowerCaseText implements PipeTransform {
  transform(value: string): string {
    const lowerCasePipe = new LowerCasePipe();
    return lowerCasePipe.transform(value);
  }
}
