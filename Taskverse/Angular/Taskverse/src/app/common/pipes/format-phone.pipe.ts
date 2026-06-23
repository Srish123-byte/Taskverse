import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'formatPhone',
  standalone: false
})
export class FormatPhone implements PipeTransform {
  transform(tel: string): string {
    const value = tel.toString().trim().replace(/^\+|-|\(|\)/g, '');

    if (value.match(/[^0-9]/)) {
      return tel;
    }

    let country: any, city: string, number: string;

    switch (value.length) {
      case 10:
        country = 1;
        city = value.slice(0, 3);
        number = value.slice(3);
        break;
      case 11:
        country = value[0];
        city = value.slice(1, 4);
        number = value.slice(4);
        break;
      case 12:
        country = value.slice(0, 3);
        city = value.slice(3, 5);
        number = value.slice(5);
        break;
      default:
        return tel;
    }

    if (country === 1 || country === '1') {
      country = '';
    }

    number = `${number.slice(0, 3)}-${number.slice(3)}`;
    return `${country} (${city}) ${number}`.trim();
  }
}
