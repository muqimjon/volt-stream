import { Directive, HostListener } from '@angular/core';

@Directive({ selector: 'input[appDecimalInput]' })
export class DecimalInputDirective {
  @HostListener('input', ['$event'])
  onInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const cleaned = input.value.replace(/[^0-9.,-]/g, '').replace(/(?!^)-/g, '');
    if (cleaned !== input.value) input.value = cleaned;
  }

  @HostListener('keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter') (event.target as HTMLInputElement).blur();
  }
}
