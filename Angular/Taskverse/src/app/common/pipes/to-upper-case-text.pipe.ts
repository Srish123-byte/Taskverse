import { UpperCasePipe } from '@angular/common';
import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'toUpperCaseText',
  standalone: false
})
export class ToUpperCaseText implements PipeTransform {
  transform(value: string): string {
    const upperCasePipe = new UpperCasePipe();
    return upperCasePipe.transform(value);
  }
}
