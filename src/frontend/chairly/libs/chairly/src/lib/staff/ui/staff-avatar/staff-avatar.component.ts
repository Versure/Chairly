import { ChangeDetectionStrategy, Component, computed, input, InputSignal } from '@angular/core';

@Component({
  selector: 'chairly-staff-avatar',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './staff-avatar.component.html',
})
export class StaffAvatarComponent {
  readonly color: InputSignal<string> = input.required<string>();
  readonly initials: InputSignal<string> = input.required<string>();
  readonly photoUrl: InputSignal<string | null> = input.required<string | null>();
  readonly size: InputSignal<'sm' | 'md' | 'lg'> = input<'sm' | 'md' | 'lg'>('md');

  protected readonly sizeClass = computed<string>(() => {
    const s = this.size();
    if (s === 'sm') return 'w-8 h-8';
    if (s === 'lg') return 'w-14 h-14';
    return 'w-10 h-10';
  });
}
