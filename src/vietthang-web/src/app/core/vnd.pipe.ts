import { Pipe, PipeTransform } from '@angular/core';

/** 275000 → "275.000 đ" */
@Pipe({ name: 'vnd', standalone: true })
export class VndPipe implements PipeTransform {
  transform(value: number | null | undefined, suffix = ' đ'): string {
    if (value == null) return '';
    return Math.round(value).toLocaleString('vi-VN') + suffix;
  }
}
