import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'chairly-access-denied',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './access-denied.component.html',
})
export class AccessDeniedComponent {}
