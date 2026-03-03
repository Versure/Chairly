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
  templateUrl: './service-list-page.component.html',
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
