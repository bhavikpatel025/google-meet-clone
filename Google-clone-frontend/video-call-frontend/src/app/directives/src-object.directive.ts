import { Directive, ElementRef, Input, OnChanges } from '@angular/core';

@Directive({
  selector: '[srcObject]',
  standalone: true
})
export class SrcObjectDirective implements OnChanges {
  @Input() srcObject?: MediaStream;

  constructor(private el: ElementRef<HTMLVideoElement>) {}

  ngOnChanges(): void {
    if (!this.el.nativeElement) {
      return;
    }

    this.el.nativeElement.srcObject = this.srcObject ?? null;
  }
}
