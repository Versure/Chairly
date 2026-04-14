import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  inject,
  output,
  viewChild,
} from '@angular/core';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';

import { DatePickerComponent } from '@org/shared-lib';

function futureDateValidator(control: AbstractControl): ValidationErrors | null {
  const value = control.value as string | null;
  if (!value) {
    return null;
  }
  const selected = new Date(value).getTime();
  const minimum = Date.now() + 60_000;
  return selected >= minimum ? null : { notFuture: true };
}

@Component({
  selector: 'chairly-schedule-newsletter-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, DatePickerComponent],
  templateUrl: './schedule-newsletter-dialog.component.html',
})
export class ScheduleNewsletterDialogComponent {
  readonly confirmed = output<string>();
  readonly cancelled = output<void>();

  private readonly document = inject(DOCUMENT);
  private readonly dialogRef = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  protected readonly form = new FormGroup({
    scheduledAtUtc: new FormControl<string>('', {
      nonNullable: true,
      validators: [Validators.required, futureDateValidator],
    }),
  });

  open(): void {
    const dialog = this.dialogRef().nativeElement;
    this.form.reset({ scheduledAtUtc: '' });
    if (!dialog.open) {
      dialog.showModal();
      this.document.body.style.overflow = 'hidden';
    }
  }

  close(): void {
    const dialog = this.dialogRef().nativeElement;
    if (dialog.open) {
      dialog.close();
    }
    this.document.body.style.overflow = '';
  }

  protected onConfirm(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const local = this.form.controls.scheduledAtUtc.value;
    const iso = new Date(local).toISOString();
    this.confirmed.emit(iso);
    this.close();
  }

  protected onCancel(): void {
    this.cancelled.emit();
    this.close();
  }
}
