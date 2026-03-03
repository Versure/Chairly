import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';

import { ServiceCategoryStore, ServiceStore } from '../../data-access';
import { ServiceCategoryResponse, ServiceResponse } from '../../models';
import { ServiceListPageComponent } from './service-list-page.component';

function makeService(overrides: Partial<ServiceResponse> = {}): ServiceResponse {
  return {
    id: 'svc-1',
    name: 'Haircut',
    description: null,
    duration: '00:30:00',
    price: 25,
    categoryId: null,
    categoryName: null,
    isActive: true,
    sortOrder: 0,
    createdAtUtc: '2026-01-01T00:00:00Z',
    createdBy: 'user',
    updatedAtUtc: null,
    updatedBy: null,
    ...overrides,
  };
}


function mockDialogs(fixture: ComponentFixture<ServiceListPageComponent>): void {
  fixture.debugElement.queryAll(By.css('dialog')).forEach((de) => {
    const el = de.nativeElement as HTMLDialogElement;
    el.showModal = vi.fn();
    el.close = vi.fn();
  });
}

describe('ServiceListPageComponent', () => {
  let fixture: ComponentFixture<ServiceListPageComponent>;
  let component: ServiceListPageComponent;

  const mockServiceStore = {
    services: signal<ServiceResponse[]>([]),
    isLoading: signal<boolean>(false),
    error: signal<string | null>(null),
    loadServices: vi.fn(),
    createService: vi.fn(),
    updateService: vi.fn(),
    deleteService: vi.fn(),
    toggleActive: vi.fn(),
    reorderServices: vi.fn(),
  };

  const mockCategoryStore = {
    categories: signal<ServiceCategoryResponse[]>([]),
    isLoading: signal<boolean>(false),
    error: signal<string | null>(null),
    loadCategories: vi.fn(),
    createCategory: vi.fn(),
    updateCategory: vi.fn(),
    deleteCategory: vi.fn(),
    reorderCategories: vi.fn(),
  };

  beforeEach(async () => {
    vi.clearAllMocks();
    mockServiceStore.services.set([]);
    mockServiceStore.isLoading.set(false);
    mockCategoryStore.categories.set([]);
    mockCategoryStore.isLoading.set(false);

    await TestBed.configureTestingModule({
      imports: [ServiceListPageComponent],
      providers: [
        { provide: ServiceStore, useValue: mockServiceStore },
        { provide: ServiceCategoryStore, useValue: mockCategoryStore },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ServiceListPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    mockDialogs(fixture);
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should call loadServices and loadCategories on init', () => {
    expect(mockServiceStore.loadServices).toHaveBeenCalledOnce();
    expect(mockCategoryStore.loadCategories).toHaveBeenCalledOnce();
  });

  it('should render the Add Service button', () => {
    const btn = fixture.debugElement.query(By.css('button'));
    expect(btn.nativeElement.textContent.trim()).toBe('Add Service');
  });

  it('should open service form dialog in create mode when Add Service is clicked', () => {
    const btn = fixture.debugElement.query(By.css('button'));
    btn.triggerEventHandler('click', null);
    fixture.detectChanges();

    const dialogs = fixture.debugElement.queryAll(By.css('dialog'));
    const formDialog = dialogs[0].nativeElement as HTMLDialogElement;
    expect(formDialog.showModal).toHaveBeenCalled();
  });

  it('should open service form dialog in edit mode when editClicked emits', () => {
    const svc = makeService();
    const tableEl = fixture.debugElement.query(
      By.css('chairly-service-table'),
    );
    tableEl.triggerEventHandler('editClicked', svc);
    fixture.detectChanges();

    const dialogs = fixture.debugElement.queryAll(By.css('dialog'));
    const formDialog = dialogs[0].nativeElement as HTMLDialogElement;
    expect(formDialog.showModal).toHaveBeenCalled();
  });

  it('should call createService when saved is emitted in create mode', () => {
    // Open in create mode (selectedService is null)
    const savedPayload = {
      name: 'New Service',
      description: null,
      duration: '00:30:00',
      price: 20,
      categoryId: null,
      sortOrder: 0,
    };
    const formDialogEl = fixture.debugElement.query(
      By.css('chairly-service-form-dialog'),
    );
    formDialogEl.triggerEventHandler('saved', savedPayload);
    expect(mockServiceStore.createService).toHaveBeenCalledWith(savedPayload);
    expect(mockServiceStore.updateService).not.toHaveBeenCalled();
  });

  it('should call updateService when saved is emitted in edit mode', () => {
    const svc = makeService();
    const tableEl = fixture.debugElement.query(
      By.css('chairly-service-table'),
    );
    // Set edit mode
    tableEl.triggerEventHandler('editClicked', svc);
    fixture.detectChanges();

    const updatedPayload = {
      name: 'Updated',
      description: null,
      duration: '00:45:00',
      price: 30,
      categoryId: null,
      sortOrder: 0,
    };
    const formDialogEl = fixture.debugElement.query(
      By.css('chairly-service-form-dialog'),
    );
    formDialogEl.triggerEventHandler('saved', updatedPayload);
    expect(mockServiceStore.updateService).toHaveBeenCalledWith(svc.id, updatedPayload);
    expect(mockServiceStore.createService).not.toHaveBeenCalled();
  });

  it('should open delete service dialog when deleteClicked emits', () => {
    const svc = makeService();
    const tableEl = fixture.debugElement.query(
      By.css('chairly-service-table'),
    );
    tableEl.triggerEventHandler('deleteClicked', svc);
    fixture.detectChanges();

    const dialogs = fixture.debugElement.queryAll(By.css('dialog'));
    // deleteServiceDialog is the second dialog (index 1)
    const deleteDialog = dialogs[1].nativeElement as HTMLDialogElement;
    expect(deleteDialog.showModal).toHaveBeenCalled();
  });

  it('should call deleteService when delete is confirmed', () => {
    const svc = makeService();
    const tableEl = fixture.debugElement.query(
      By.css('chairly-service-table'),
    );
    tableEl.triggerEventHandler('deleteClicked', svc);

    const deleteDialogEl = fixture.debugElement.queryAll(
      By.css('chairly-confirmation-dialog'),
    )[0];
    deleteDialogEl.triggerEventHandler('confirmed', null);

    expect(mockServiceStore.deleteService).toHaveBeenCalledWith(svc.id);
  });

  it('should call toggleActive when toggleActiveClicked emits', () => {
    const svc = makeService();
    const tableEl = fixture.debugElement.query(
      By.css('chairly-service-table'),
    );
    tableEl.triggerEventHandler('toggleActiveClicked', svc);
    expect(mockServiceStore.toggleActive).toHaveBeenCalledWith(svc.id);
  });

  it('should call createCategory when categoryCreated emits', () => {
    const request = { name: 'New Cat', sortOrder: 0 };
    const panelEl = fixture.debugElement.query(
      By.css('chairly-category-panel'),
    );
    panelEl.triggerEventHandler('categoryCreated', request);
    expect(mockCategoryStore.createCategory).toHaveBeenCalledWith(request);
  });

  it('should call updateCategory when categoryUpdated emits', () => {
    const event = { id: 'cat-1', request: { name: 'Updated Cat', sortOrder: 1 } };
    const panelEl = fixture.debugElement.query(
      By.css('chairly-category-panel'),
    );
    panelEl.triggerEventHandler('categoryUpdated', event);
    expect(mockCategoryStore.updateCategory).toHaveBeenCalledWith(
      event.id,
      event.request,
    );
  });

  it('should open delete category dialog when categoryDeleted emits', () => {
    const panelEl = fixture.debugElement.query(
      By.css('chairly-category-panel'),
    );
    panelEl.triggerEventHandler('categoryDeleted', 'cat-1');
    fixture.detectChanges();

    const dialogs = fixture.debugElement.queryAll(By.css('dialog'));
    // deleteCategoryDialog is the third dialog (index 2)
    const deleteCatDialog = dialogs[2].nativeElement as HTMLDialogElement;
    expect(deleteCatDialog.showModal).toHaveBeenCalled();
  });

  it('should call deleteCategory when category delete is confirmed', () => {
    const panelEl = fixture.debugElement.query(
      By.css('chairly-category-panel'),
    );
    panelEl.triggerEventHandler('categoryDeleted', 'cat-1');

    const confirmDialogs = fixture.debugElement.queryAll(
      By.css('chairly-confirmation-dialog'),
    );
    // Second confirmation dialog is for category delete
    confirmDialogs[1].triggerEventHandler('confirmed', null);

    expect(mockCategoryStore.deleteCategory).toHaveBeenCalledWith('cat-1');
  });

  it('should call reorderCategories when categoriesReordered emits', () => {
    const ordered: ServiceCategoryResponse[] = [
      { id: 'cat-1', name: 'Hair', sortOrder: 0, createdAtUtc: '2026-01-01T00:00:00Z', createdBy: 'user' },
    ];
    const panelEl = fixture.debugElement.query(By.css('chairly-category-panel'));
    panelEl.triggerEventHandler('categoriesReordered', ordered);

    expect(mockCategoryStore.reorderCategories).toHaveBeenCalledWith(ordered);
  });

  it('should call reorderServices when servicesReordered emits', () => {
    const ordered = [makeService()];
    const tableEl = fixture.debugElement.query(By.css('chairly-service-table'));
    tableEl.triggerEventHandler('servicesReordered', ordered);

    expect(mockServiceStore.reorderServices).toHaveBeenCalledWith(ordered);
  });
});
