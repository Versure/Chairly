import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  OnInit,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { ConfirmationDialogComponent, LoadingIndicatorComponent } from '@org/shared-lib';

import { StaffApiService, StaffStore } from '../../data-access';
import { CreateStaffMemberRequest, StaffMemberResponse } from '../../models';
import { StaffFormDialogComponent, StaffTableComponent } from '../../ui';

@Component({
  selector: 'chairly-staff-list-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ConfirmationDialogComponent, LoadingIndicatorComponent, StaffFormDialogComponent, StaffTableComponent],
  templateUrl: './staff-list-page.component.html',
})
export class StaffListPageComponent implements OnInit {
  private readonly store = inject(StaffStore);
  private readonly staffApi = inject(StaffApiService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly formDialogRef = viewChild.required(StaffFormDialogComponent);
  private readonly deactivateDialogRef =
    viewChild.required<ConfirmationDialogComponent>('deactivateDialog');

  protected readonly selectedStaff = signal<StaffMemberResponse | null>(null);
  protected readonly staffMembers = this.store.staffMembers;
  protected readonly isLoading = this.store.isLoading;
  protected readonly mutationError = signal<string | null>(null);

  ngOnInit(): void {
    this.store.loadAll();
  }

  protected openAddDialog(): void {
    this.selectedStaff.set(null);
    this.formDialogRef().open(null);
  }

  protected onEdit(member: StaffMemberResponse): void {
    this.selectedStaff.set(member);
    this.formDialogRef().open(member);
  }

  protected onDeactivate(member: StaffMemberResponse): void {
    this.selectedStaff.set(member);
    this.deactivateDialogRef().open();
  }

  protected onReactivate(member: StaffMemberResponse): void {
    this.mutationError.set(null);
    this.staffApi
      .reactivate(member.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.store.reactivateStaffMember(member.id);
        },
        error: () => {
          this.mutationError.set('Er is een fout opgetreden. Probeer het opnieuw.');
        },
      });
  }

  protected onConfirmDeactivate(): void {
    const member = this.selectedStaff();
    this.mutationError.set(null);
    if (member) {
      this.staffApi
        .deactivate(member.id)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.store.deactivateStaffMember(member.id);
          },
          error: () => {
            this.mutationError.set('Er is een fout opgetreden. Probeer het opnieuw.');
          },
        });
    }
    this.selectedStaff.set(null);
  }

  protected onSave(request: CreateStaffMemberRequest): void {
    const member = this.selectedStaff();
    this.mutationError.set(null);
    this.selectedStaff.set(null);
    if (member) {
      this.staffApi
        .update(member.id, request)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (updated) => {
            this.store.updateStaffMember(updated);
          },
          error: () => {
            this.mutationError.set('Er is een fout opgetreden. Probeer het opnieuw.');
          },
        });
    } else {
      this.staffApi
        .create(request)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (created) => {
            this.store.addStaffMember(created);
          },
          error: () => {
            this.mutationError.set('Er is een fout opgetreden. Probeer het opnieuw.');
          },
        });
    }
  }

  protected onCancelled(): void {
    this.selectedStaff.set(null);
  }
}
