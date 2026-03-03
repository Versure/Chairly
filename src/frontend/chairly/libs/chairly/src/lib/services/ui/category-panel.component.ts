import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  output,
  OutputEmitterRef,
  signal,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

import {
  CreateServiceCategoryRequest,
  ServiceCategoryResponse,
  UpdateServiceCategoryRequest,
} from '../models';

@Component({
  selector: 'chairly-category-panel',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  templateUrl: './category-panel.component.html',
})
export class CategoryPanelComponent {
  readonly categories = input.required<ServiceCategoryResponse[]>();
  readonly isLoading = input.required<boolean>();

  readonly categoryCreated: OutputEmitterRef<CreateServiceCategoryRequest> =
    output<CreateServiceCategoryRequest>();
  readonly categoryUpdated: OutputEmitterRef<{
    id: string;
    request: UpdateServiceCategoryRequest;
  }> = output<{ id: string; request: UpdateServiceCategoryRequest }>();
  readonly categoryDeleted: OutputEmitterRef<string> = output<string>();
  readonly categoriesReordered: OutputEmitterRef<ServiceCategoryResponse[]> =
    output<ServiceCategoryResponse[]>();

  protected readonly sortedCategories = computed<ServiceCategoryResponse[]>(() =>
    [...this.categories()].sort((a, b) => a.sortOrder - b.sortOrder),
  );

  protected readonly showAddForm = signal<boolean>(false);
  protected readonly editingCategoryId = signal<string | null>(null);
  protected readonly draggedIndex = signal<number | null>(null);

  protected readonly addForm = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(150)],
    }),
  });

  protected readonly editForm = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(150)],
    }),
  });

  protected startAdding(): void {
    this.addForm.reset({ name: '' });
    this.showAddForm.set(true);
    this.editingCategoryId.set(null);
  }

  protected cancelAdding(): void {
    this.showAddForm.set(false);
    this.addForm.reset();
  }

  protected saveNewCategory(): void {
    if (this.addForm.invalid) {
      return;
    }
    const { name } = this.addForm.getRawValue();
    this.categoryCreated.emit({ name, sortOrder: this.categories().length });
    this.showAddForm.set(false);
    this.addForm.reset();
  }

  protected startEdit(category: ServiceCategoryResponse): void {
    this.editForm.reset({ name: category.name });
    this.editingCategoryId.set(category.id);
    this.showAddForm.set(false);
  }

  protected cancelEdit(): void {
    this.editingCategoryId.set(null);
    this.editForm.reset();
  }

  protected saveEdit(id: string): void {
    if (this.editForm.invalid) {
      return;
    }
    const { name } = this.editForm.getRawValue();
    const category = this.categories().find((c) => c.id === id);
    const sortOrder = category?.sortOrder ?? 0;
    this.categoryUpdated.emit({ id, request: { name, sortOrder } });
    this.editingCategoryId.set(null);
    this.editForm.reset();
  }

  protected onDeleteClicked(id: string): void {
    this.categoryDeleted.emit(id);
  }

  protected onDragStart(index: number): void {
    this.draggedIndex.set(index);
  }

  protected onDragOver(event: DragEvent): void {
    event.preventDefault();
  }

  protected onDrop(dropIndex: number): void {
    const fromIndex = this.draggedIndex();
    if (fromIndex === null || fromIndex === dropIndex) {
      this.draggedIndex.set(null);
      return;
    }
    const ordered = [...this.sortedCategories()];
    const [removed] = ordered.splice(fromIndex, 1);
    ordered.splice(dropIndex, 0, removed);
    this.draggedIndex.set(null);
    this.categoriesReordered.emit(ordered);
  }
}
