import {
  ChangeDetectionStrategy,
  Component,
  inject,
  OnDestroy,
  OnInit,
  signal,
  viewChild,
} from '@angular/core';

import { Subject, takeUntil } from 'rxjs';

import { ConfirmationDialogComponent } from '@org/shared-lib';

import { StaffApiService, StaffStore } from '../../data-access';
import { CreateStaffMemberRequest, StaffMemberResponse } from '../../models';
import { StaffFormDialogComponent, StaffTableComponent } from '../../ui';

@Component({
  selector: 'chairly-staff-list-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ConfirmationDialogComponent, StaffFormDialogComponent, StaffTableComponent],
  templateUrl: './staff-list-page.component.html',
})
export class StaffListPageComponent implements OnInit, OnDestroy {
  private readonly store = inject(StaffStore);
  private readonly staffApi = inject(StaffApiService);
  private readonly destroy$ = new Subject<void>();

  private readonly formDialogRef = viewChild.required(StaffFormDialogComponent);
  private readonly deactivateDialogRef =
    viewChild.required<ConfirmationDialogComponent>('deactivateDialog');

  protected readonly selectedStaff = signal<StaffMemberResponse | null>(null);
  protected readonly staffMembers = this.store.staffMembers;
  protected readonly isLoading = this.store.isLoading;

  ngOnInit(): void {
    this.store.loadAll();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
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
    this.staffApi
      .reactivate(member.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.store.reactivateStaffMember(member.id);
      });
  }

  protected onConfirmDeactivate(): void {
    const member = this.selectedStaff();
    if (member) {
      this.staffApi
        .deactivate(member.id)
        .pipe(takeUntil(this.destroy$))
        .subscribe(() => {
          this.store.deactivateStaffMember(member.id);
        });
    }
    this.selectedStaff.set(null);
  }

  protected onSave(request: CreateStaffMemberRequest): void {
    const member = this.selectedStaff();
    this.selectedStaff.set(null);
    if (member) {
      this.staffApi
        .update(member.id, request)
        .pipe(takeUntil(this.destroy$))
        .subscribe((updated) => {
          this.store.updateStaffMember(updated);
        });
    } else {
      this.staffApi
        .create(request)
        .pipe(takeUntil(this.destroy$))
        .subscribe((created) => {
          this.store.addStaffMember(created);
        });
    }
  }

  protected onCancelled(): void {
    this.selectedStaff.set(null);
  }
}
