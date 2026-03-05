import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  inject,
  input,
  InputSignal,
  output,
  OutputEmitterRef,
  signal,
  viewChild,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

import {
  CreateStaffMemberRequest,
  StaffMemberResponse,
  StaffRole,
  WeeklySchedule,
} from '../models';

@Component({
  selector: 'chairly-staff-form-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  templateUrl: './staff-form-dialog.component.html',
})
export class StaffFormDialogComponent {
  readonly staffMember: InputSignal<StaffMemberResponse | null> = input<StaffMemberResponse | null>(null);

  readonly saved: OutputEmitterRef<CreateStaffMemberRequest> = output<CreateStaffMemberRequest>();
  readonly cancelled: OutputEmitterRef<void> = output<void>();

  private readonly document = inject(DOCUMENT);
  private readonly dialogRef = viewChild.required<ElementRef<HTMLDialogElement>>('dialogEl');

  protected readonly scheduleSignal = signal<WeeklySchedule>({});
  protected readonly selectedColor = signal('#6366f1');

  protected readonly colorPalette: readonly string[] = [
    '#6366f1', '#8b5cf6', '#ec4899', '#ef4444', '#f97316',
    '#eab308', '#22c55e', '#14b8a6', '#3b82f6', '#64748b',
  ];

  protected readonly form = new FormGroup({
    firstName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(100)],
    }),
    lastName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(100)],
    }),
    role: new FormControl<StaffRole>('staff_member', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    color: new FormControl('#6366f1', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    photoUrl: new FormControl<string | null>(null),
  });

  open(): void {
    const member = this.staffMember();
    if (member) {
      this.form.reset({
        firstName: member.firstName,
        lastName: member.lastName,
        role: member.role,
        color: member.color,
        photoUrl: member.photoUrl,
      });
      this.selectedColor.set(member.color);
      this.scheduleSignal.set(member.schedule);
    } else {
      this.form.reset({
        firstName: '',
        lastName: '',
        role: 'staff_member',
        color: '#6366f1',
        photoUrl: null,
      });
      this.selectedColor.set('#6366f1');
      this.scheduleSignal.set({});
    }
    this.document.body.style.overflow = 'hidden';
    this.dialogRef().nativeElement.showModal();
  }

  close(): void {
    this.document.body.style.overflow = '';
    this.dialogRef().nativeElement.close();
  }

  protected selectColor(color: string): void {
    this.form.controls.color.setValue(color);
    this.selectedColor.set(color);
  }

  protected onSave(): void {
    if (this.form.invalid) {
      return;
    }
    const { firstName, lastName, role, color, photoUrl } = this.form.getRawValue();
    this.close();
    this.saved.emit({
      firstName,
      lastName,
      role,
      color,
      photoUrl,
      schedule: this.scheduleSignal(),
    });
  }

  protected onCancel(): void {
    this.close();
    this.cancelled.emit();
  }
}
