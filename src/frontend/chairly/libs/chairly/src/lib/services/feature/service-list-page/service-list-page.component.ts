import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnInit,
  signal,
  viewChild,
} from '@angular/core';

import { ConfirmationDialogComponent } from '@org/shared-lib';

import { ServiceCategoryStore, ServiceStore } from '../../data-access';
import {
  CreateServiceCategoryRequest,
  CreateServiceRequest,
  ServiceCategoryResponse,
  ServiceResponse,
  UpdateServiceCategoryRequest,
  UpdateServiceRequest,
} from '../../models';
import { CategoryPanelComponent, ServiceFormDialogComponent, ServiceTableComponent } from '../../ui';

@Component({
  selector: 'chairly-service-list-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CategoryPanelComponent,
    ConfirmationDialogComponent,
    ServiceFormDialogComponent,
    ServiceTableComponent,
  ],
  template: `
    <div class="flex min-h-screen flex-col bg-gray-50">
      <!-- Page header -->
      <div class="flex items-center justify-between border-b border-gray-200 bg-white px-6 py-4">
        <h1 class="text-xl font-semibold text-gray-900">Services</h1>
        <button
          type="button"
          class="inline-flex items-center rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2"
          (click)="openAddService()"
        >
          Add Service
        </button>
      </div>

      <!-- Main layout: table + sidebar -->
      <div class="flex flex-1 overflow-hidden">
        <!-- Services table -->
        <div class="flex-1 overflow-auto p-6">
          <chairly-service-table
            [services]="services()"
            [isLoading]="servicesLoading()"
            (editClicked)="onEditService($event)"
            (deleteClicked)="onDeleteService($event)"
            (toggleActiveClicked)="onToggleActive($event)"
          />
        </div>

        <!-- Category sidebar -->
        <div class="w-72 flex-shrink-0 overflow-auto border-l border-gray-200 p-4">
          <chairly-category-panel
            [categories]="categories()"
            [isLoading]="categoriesLoading()"
            (categoryCreated)="onCategoryCreated($event)"
            (categoryUpdated)="onCategoryUpdated($event)"
            (categoryDeleted)="onCategoryDeleted($event)"
          />
        </div>
      </div>
    </div>

    <!-- Service form dialog (used for both create and edit) -->
    <chairly-service-form-dialog
      [categories]="categories()"
      [service]="selectedService()"
      (saved)="onServiceSaved($event)"
      (cancelled)="onServiceFormCancelled()"
    />

    <!-- Delete service confirmation dialog -->
    <chairly-confirmation-dialog
      #deleteServiceDialog
      title="Delete Service"
      message="Are you sure you want to delete this service? This action cannot be undone."
      confirmLabel="Delete"
      [isDestructive]="true"
      (confirmed)="onConfirmDeleteService()"
    />

    <!-- Delete category confirmation dialog -->
    <chairly-confirmation-dialog
      #deleteCategoryDialog
      title="Delete Category"
      message="Are you sure you want to delete this category? This action cannot be undone."
      confirmLabel="Delete"
      [isDestructive]="true"
      (confirmed)="onConfirmDeleteCategory()"
    />
  `,
})
export class ServiceListPageComponent implements OnInit {
  private readonly serviceStore = inject(ServiceStore);
  private readonly categoryStore = inject(ServiceCategoryStore);

  private readonly serviceFormDialogRef = viewChild.required(ServiceFormDialogComponent);
  private readonly deleteServiceDialogRef =
    viewChild.required<ConfirmationDialogComponent>('deleteServiceDialog');
  private readonly deleteCategoryDialogRef =
    viewChild.required<ConfirmationDialogComponent>('deleteCategoryDialog');

  protected readonly selectedService = signal<ServiceResponse | null>(null);
  private readonly selectedCategoryIdForDelete = signal<string | null>(null);

  protected readonly services = computed<ServiceResponse[]>(() =>
    this.serviceStore.services(),
  );
  protected readonly servicesLoading = computed<boolean>(() =>
    this.serviceStore.isLoading(),
  );
  protected readonly categories = computed<ServiceCategoryResponse[]>(() =>
    this.categoryStore.categories(),
  );
  protected readonly categoriesLoading = computed<boolean>(() =>
    this.categoryStore.isLoading(),
  );

  ngOnInit(): void {
    this.serviceStore.loadServices();
    this.categoryStore.loadCategories();
  }

  protected openAddService(): void {
    this.selectedService.set(null);
    this.serviceFormDialogRef().open();
  }

  protected onEditService(service: ServiceResponse): void {
    this.selectedService.set(service);
    this.serviceFormDialogRef().open();
  }

  protected onServiceSaved(request: CreateServiceRequest | UpdateServiceRequest): void {
    const svc = this.selectedService();
    if (svc) {
      this.serviceStore.updateService(svc.id, request as UpdateServiceRequest);
    } else {
      this.serviceStore.createService(request as CreateServiceRequest);
    }
    this.selectedService.set(null);
  }

  protected onServiceFormCancelled(): void {
    this.selectedService.set(null);
  }

  protected onDeleteService(service: ServiceResponse): void {
    this.selectedService.set(service);
    this.deleteServiceDialogRef().open();
  }

  protected onConfirmDeleteService(): void {
    const svc = this.selectedService();
    if (svc) {
      this.serviceStore.deleteService(svc.id);
    }
    this.selectedService.set(null);
  }

  protected onToggleActive(service: ServiceResponse): void {
    this.serviceStore.toggleActive(service.id);
  }

  protected onCategoryCreated(request: CreateServiceCategoryRequest): void {
    this.categoryStore.createCategory(request);
  }

  protected onCategoryUpdated(event: {
    id: string;
    request: UpdateServiceCategoryRequest;
  }): void {
    this.categoryStore.updateCategory(event.id, event.request);
  }

  protected onCategoryDeleted(id: string): void {
    this.selectedCategoryIdForDelete.set(id);
    this.deleteCategoryDialogRef().open();
  }

  protected onConfirmDeleteCategory(): void {
    const id = this.selectedCategoryIdForDelete();
    if (id) {
      this.categoryStore.deleteCategory(id);
    }
    this.selectedCategoryIdForDelete.set(null);
  }
}
