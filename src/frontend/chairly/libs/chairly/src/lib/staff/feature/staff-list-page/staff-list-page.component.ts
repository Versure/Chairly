import { HttpErrorResponse } from '@angular/common/http';
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

import {
  ConfirmationDialogComponent,
  LoadingIndicatorComponent,
  PageHeaderComponent,
} from '@org/shared-lib';

import { StaffApiService, StaffStore } from '../../data-access';
import { CreateStaffMemberRequest, StaffMemberResponse } from '../../models';
import { StaffFormDialogComponent, StaffTableComponent } from '../../ui';

@Component({
  selector: 'chairly-staff-list-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ConfirmationDialogComponent,
    LoadingIndicatorComponent,
    PageHeaderComponent,
    StaffFormDialogComponent,
    StaffTableComponent,
  ],
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
  protected readonly apiEmailError = signal<string | null>(null);

  private getApiErrors(errorBody: unknown): Record<string, string[]> {
    if (!errorBody || typeof errorBody !== 'object') {
      return {};
    }

    const errors = (errorBody as Record<string, unknown>)['errors'];
    if (!errors || typeof errors !== 'object') {
      return {};
    }

    const normalizedErrors: Record<string, string[]> = {};
    for (const [key, value] of Object.entries(errors as Record<string, unknown>)) {
      if (Array.isArray(value)) {
        normalizedErrors[key.toLowerCase()] = value.filter(
          (message): message is string => typeof message === 'string' && message.trim().length > 0,
        );
      }
    }

    return normalizedErrors;
  }

  private mapCreateError(error: unknown): {
    formMessage: string | null;
    emailMessage: string | null;
  } {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 401) {
        return {
          formMessage: 'Je sessie is verlopen of ongeldig. Log opnieuw in en probeer het nogmaals.',
          emailMessage: null,
        };
      }

      if (error.status === 403) {
        return {
          formMessage: 'Je hebt geen rechten om deze actie uit te voeren.',
          emailMessage: null,
        };
      }

      if (error.status === 400 || error.status === 422) {
        const errors = this.getApiErrors(error.error);
        const hasEmailError = Boolean(errors['email']?.length);
        if (hasEmailError) {
          return {
            formMessage: 'Controleer de ingevulde gegevens en probeer het opnieuw.',
            emailMessage:
              'Controleer het e-mailadres. Dit veld is verplicht en moet een geldig formaat hebben.',
          };
        }

        return {
          formMessage: 'Controleer de ingevulde gegevens en probeer het opnieuw.',
          emailMessage: null,
        };
      }
    }

    return { formMessage: 'Er is een fout opgetreden. Probeer het opnieuw.', emailMessage: null };
  }

  ngOnInit(): void {
    this.store.loadAll();
  }

  protected openAddDialog(): void {
    this.selectedStaff.set(null);
    this.mutationError.set(null);
    this.apiEmailError.set(null);
    this.formDialogRef().open(null);
  }

  protected onEdit(member: StaffMemberResponse): void {
    this.selectedStaff.set(member);
    this.mutationError.set(null);
    this.apiEmailError.set(null);
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
    this.apiEmailError.set(null);
    if (member) {
      this.staffApi
        .update(member.id, request)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (updated) => {
            this.formDialogRef().close();
            this.store.updateStaffMember(updated);
            this.selectedStaff.set(null);
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
            this.formDialogRef().close();
            this.store.addStaffMember(created);
            this.selectedStaff.set(null);
          },
          error: (error: unknown) => {
            const mappedError = this.mapCreateError(error);
            this.mutationError.set(mappedError.formMessage);
            this.apiEmailError.set(mappedError.emailMessage);
          },
        });
    }
  }

  protected onCancelled(): void {
    this.selectedStaff.set(null);
    this.apiEmailError.set(null);
  }
}
