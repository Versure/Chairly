import { DatePipe, TitleCasePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnInit,
  signal,
  viewChild,
} from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { AdminSubscriptionStore } from '../../data-access';
import { SubscriptionListFilters, UpdateSubscriptionPlanPayload } from '../../models';
import { BillingCyclePipe, SubscriptionStatusBadgePipe } from '../../pipes';
import {
  CancelSubscriptionDialogComponent,
  ProvisionSubscriptionDialogComponent,
  UpdatePlanDialogComponent,
} from '../../ui';

@Component({
  selector: 'chairly-admin-subscription-detail-page',
  standalone: true,
  imports: [
    DatePipe,
    TitleCasePipe,
    RouterLink,
    BillingCyclePipe,
    SubscriptionStatusBadgePipe,
    ProvisionSubscriptionDialogComponent,
    CancelSubscriptionDialogComponent,
    UpdatePlanDialogComponent,
  ],
  templateUrl: './subscription-detail-page.component.html',
  styleUrl: './subscription-detail-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SubscriptionDetailPageComponent implements OnInit {
  private readonly store = inject(AdminSubscriptionStore);
  private readonly route = inject(ActivatedRoute);
  protected readonly router = inject(Router);

  private readonly provisionDialogRef =
    viewChild.required<ProvisionSubscriptionDialogComponent>('provisionDialog');
  private readonly cancelDialogRef =
    viewChild.required<CancelSubscriptionDialogComponent>('cancelDialog');
  private readonly updatePlanDialogRef =
    viewChild.required<UpdatePlanDialogComponent>('updatePlanDialog');

  protected readonly subscription = computed(() => this.store.selectedSubscription());
  protected readonly isDetailLoading = computed(() => this.store.isDetailLoading());
  protected readonly isSubmitting = signal(false);

  private subscriptionId = '';

  private readonly defaultFilters: SubscriptionListFilters = {
    search: '',
    status: '',
    plan: '',
    page: 1,
    pageSize: 25,
  };

  ngOnInit(): void {
    this.subscriptionId = this.route.snapshot.params['id'] as string;
    this.store.loadSubscription(this.subscriptionId);
  }

  protected openProvisionDialog(): void {
    this.provisionDialogRef().open();
  }

  protected openCancelDialog(): void {
    this.cancelDialogRef().open();
  }

  protected openUpdatePlanDialog(): void {
    this.updatePlanDialogRef().open();
  }

  protected onProvisionConfirm(): void {
    this.isSubmitting.set(true);
    this.store.provisionSubscription(this.subscriptionId, this.defaultFilters);
    this.provisionDialogRef().close();
    this.isSubmitting.set(false);
  }

  protected onCancelConfirm(reason: string): void {
    this.isSubmitting.set(true);
    this.store.cancelSubscription(
      this.subscriptionId,
      { cancellationReason: reason },
      this.defaultFilters,
    );
    this.cancelDialogRef().close();
    this.isSubmitting.set(false);
  }

  protected onUpdatePlanConfirm(payload: UpdateSubscriptionPlanPayload): void {
    this.isSubmitting.set(true);
    this.store.updateSubscriptionPlan(this.subscriptionId, payload, this.defaultFilters);
    this.updatePlanDialogRef().close();
    this.isSubmitting.set(false);
  }

  protected onDialogCancel(): void {
    // No action needed, dialog closes itself
  }
}
