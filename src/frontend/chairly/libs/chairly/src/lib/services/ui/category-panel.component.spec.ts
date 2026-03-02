import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CreateServiceCategoryRequest, ServiceCategoryResponse, UpdateServiceCategoryRequest } from '../util';
import { CategoryPanelComponent } from './category-panel.component';

const mockCategories: ServiceCategoryResponse[] = [
  { id: '2', name: 'Nails', sortOrder: 2, createdAtUtc: '2026-01-01T00:00:00Z', createdBy: 'user' },
  { id: '1', name: 'Hair', sortOrder: 1, createdAtUtc: '2026-01-01T00:00:00Z', createdBy: 'user' },
];

describe('CategoryPanelComponent', () => {
  let component: CategoryPanelComponent;
  let fixture: ComponentFixture<CategoryPanelComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CategoryPanelComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(CategoryPanelComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('categories', mockCategories);
    fixture.componentRef.setInput('isLoading', false);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display categories sorted by sortOrder', () => {
    const items = fixture.nativeElement.querySelectorAll('li') as NodeListOf<HTMLLIElement>;
    expect(items[0].textContent).toContain('Hair');
    expect(items[1].textContent).toContain('Nails');
  });

  it('should show loading indicator when isLoading is true', () => {
    fixture.componentRef.setInput('isLoading', true);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Loading...');
  });

  it('should hide category list when isLoading is true', () => {
    fixture.componentRef.setInput('isLoading', true);
    fixture.detectChanges();

    const list = fixture.nativeElement.querySelector('ul') as HTMLUListElement | null;
    expect(list).toBeNull();
  });

  it('should show add form when Add button is clicked', () => {
    const addButton = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    addButton.click();
    fixture.detectChanges();

    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement | null;
    expect(form).toBeTruthy();
  });

  it('should hide add form when Cancel is clicked', () => {
    const addButton = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    addButton.click();
    fixture.detectChanges();

    const cancelButton = fixture.nativeElement.querySelector(
      'form button[type="button"]',
    ) as HTMLButtonElement;
    cancelButton.click();
    fixture.detectChanges();

    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement | null;
    expect(form).toBeNull();
  });

  it('should emit categoryCreated when add form is submitted with valid data', () => {
    let emitted: CreateServiceCategoryRequest | undefined;
    component.categoryCreated.subscribe((req) => {
      emitted = req;
    });

    const addButton = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    addButton.click();
    fixture.detectChanges();

    const inputs = fixture.nativeElement.querySelectorAll(
      'form input',
    ) as NodeListOf<HTMLInputElement>;
    inputs[0].value = 'New Category';
    inputs[0].dispatchEvent(new Event('input'));
    inputs[1].value = '5';
    inputs[1].dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(emitted).toEqual({ name: 'New Category', sortOrder: 5 });
  });

  it('should not emit categoryCreated when add form is submitted with empty name', () => {
    let emitted = false;
    component.categoryCreated.subscribe(() => {
      emitted = true;
    });

    const addButton = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    addButton.click();
    fixture.detectChanges();

    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(emitted).toBe(false);
  });

  it('should show inline edit form when Edit button is clicked', () => {
    const editButton = fixture.nativeElement.querySelector(
      '[title="Edit category"]',
    ) as HTMLButtonElement;
    editButton.click();
    fixture.detectChanges();

    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement | null;
    expect(form).toBeTruthy();
  });

  it('should pre-populate edit form with category values', () => {
    const editButton = fixture.nativeElement.querySelector(
      '[title="Edit category"]',
    ) as HTMLButtonElement;
    editButton.click();
    fixture.detectChanges();

    const inputs = fixture.nativeElement.querySelectorAll(
      'form input',
    ) as NodeListOf<HTMLInputElement>;
    expect(inputs[0].value).toBe('Hair');
  });

  it('should emit categoryUpdated when edit form is submitted', () => {
    let updated: { id: string; request: UpdateServiceCategoryRequest } | undefined;
    component.categoryUpdated.subscribe((u) => {
      updated = u;
    });

    const editButton = fixture.nativeElement.querySelector(
      '[title="Edit category"]',
    ) as HTMLButtonElement;
    editButton.click();
    fixture.detectChanges();

    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(updated?.id).toBe('1');
    expect(updated?.request.name).toBe('Hair');
    expect(updated?.request.sortOrder).toBe(1);
  });

  it('should hide edit form when Cancel is clicked', () => {
    const editButton = fixture.nativeElement.querySelector(
      '[title="Edit category"]',
    ) as HTMLButtonElement;
    editButton.click();
    fixture.detectChanges();

    const cancelButton = fixture.nativeElement.querySelector(
      'form button[type="button"]',
    ) as HTMLButtonElement;
    cancelButton.click();
    fixture.detectChanges();

    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement | null;
    expect(form).toBeNull();
  });

  it('should emit categoryDeleted when Delete button is clicked', () => {
    let deletedId: string | undefined;
    component.categoryDeleted.subscribe((id) => {
      deletedId = id;
    });

    const deleteButton = fixture.nativeElement.querySelector(
      '[title="Delete category"]',
    ) as HTMLButtonElement;
    deleteButton.click();
    fixture.detectChanges();

    expect(deletedId).toBe('1');
  });

  it('should show empty state when no categories exist', () => {
    fixture.componentRef.setInput('categories', []);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No categories yet.');
  });
});
