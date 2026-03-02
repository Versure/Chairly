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
} from '../util';

@Component({
  selector: 'chairly-category-panel',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  template: `
    <div class="flex flex-col bg-white border border-gray-200 rounded-lg overflow-hidden">
      <!-- Header -->
      <div class="flex items-center justify-between px-4 py-3 border-b border-gray-200 bg-gray-50">
        <h3 class="text-sm font-semibold text-gray-700 uppercase tracking-wide">Categories</h3>
        <button
          type="button"
          class="text-xs text-indigo-600 hover:text-indigo-700 font-medium disabled:opacity-50 disabled:cursor-not-allowed"
          [disabled]="showAddForm()"
          (click)="startAdding()"
        >
          + Add
        </button>
      </div>

      <!-- Add form -->
      @if (showAddForm()) {
        <form
          class="p-3 border-b border-gray-200 bg-indigo-50"
          [formGroup]="addForm"
          (ngSubmit)="saveNewCategory()"
        >
          <div class="flex flex-col gap-2">
            <input
              formControlName="name"
              type="text"
              placeholder="Category name"
              class="text-sm border border-gray-300 rounded px-2 py-1 focus:outline-none focus:ring-1 focus:ring-indigo-500"
            />
            <input
              formControlName="sortOrder"
              type="number"
              placeholder="Sort order"
              class="text-sm border border-gray-300 rounded px-2 py-1 focus:outline-none focus:ring-1 focus:ring-indigo-500"
            />
            <div class="flex gap-2">
              <button
                type="submit"
                [disabled]="addForm.invalid"
                class="flex-1 text-xs bg-indigo-600 text-white rounded py-1 hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Save
              </button>
              <button
                type="button"
                class="flex-1 text-xs bg-white border border-gray-300 text-gray-700 rounded py-1 hover:bg-gray-50"
                (click)="cancelAdding()"
              >
                Cancel
              </button>
            </div>
          </div>
        </form>
      }

      <!-- Loading state -->
      @if (isLoading()) {
        <div class="p-4 text-sm text-gray-500 text-center">Loading...</div>
      }

      <!-- Category list -->
      @if (!isLoading()) {
        <ul class="divide-y divide-gray-100">
          @for (category of sortedCategories(); track category.id) {
            <li class="px-3 py-2">
              @if (editingCategoryId() === category.id) {
                <form
                  class="flex flex-col gap-2"
                  [formGroup]="editForm"
                  (ngSubmit)="saveEdit(category.id)"
                >
                  <input
                    formControlName="name"
                    type="text"
                    class="text-sm border border-gray-300 rounded px-2 py-1 focus:outline-none focus:ring-1 focus:ring-indigo-500"
                  />
                  <input
                    formControlName="sortOrder"
                    type="number"
                    class="text-sm border border-gray-300 rounded px-2 py-1 focus:outline-none focus:ring-1 focus:ring-indigo-500"
                  />
                  <div class="flex gap-2">
                    <button
                      type="submit"
                      [disabled]="editForm.invalid"
                      class="flex-1 text-xs bg-indigo-600 text-white rounded py-1 hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                      Save
                    </button>
                    <button
                      type="button"
                      class="flex-1 text-xs bg-white border border-gray-300 text-gray-700 rounded py-1 hover:bg-gray-50"
                      (click)="cancelEdit()"
                    >
                      Cancel
                    </button>
                  </div>
                </form>
              } @else {
                <div class="flex items-center justify-between">
                  <div class="flex flex-col min-w-0">
                    <span class="text-sm font-medium text-gray-900 truncate">{{ category.name }}</span>
                    <span class="text-xs text-gray-400">Order: {{ category.sortOrder }}</span>
                  </div>
                  <div class="flex items-center gap-1 flex-shrink-0">
                    <button
                      type="button"
                      class="px-2 py-1 text-xs text-gray-500 hover:text-indigo-600 hover:bg-indigo-50 rounded"
                      title="Edit category"
                      (click)="startEdit(category)"
                    >
                      Edit
                    </button>
                    <button
                      type="button"
                      class="px-2 py-1 text-xs text-gray-500 hover:text-red-600 hover:bg-red-50 rounded"
                      title="Delete category"
                      (click)="onDeleteClicked(category.id)"
                    >
                      Delete
                    </button>
                  </div>
                </div>
              }
            </li>
          } @empty {
            <li class="p-4 text-sm text-gray-400 text-center">No categories yet.</li>
          }
        </ul>
      }
    </div>
  `,
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

  protected readonly sortedCategories = computed<ServiceCategoryResponse[]>(() =>
    [...this.categories()].sort((a, b) => a.sortOrder - b.sortOrder),
  );

  protected readonly showAddForm = signal<boolean>(false);
  protected readonly editingCategoryId = signal<string | null>(null);

  protected readonly addForm = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(150)],
    }),
    sortOrder: new FormControl(0, { nonNullable: true }),
  });

  protected readonly editForm = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(150)],
    }),
    sortOrder: new FormControl(0, { nonNullable: true }),
  });

  protected startAdding(): void {
    this.addForm.reset({ name: '', sortOrder: 0 });
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
    const { name, sortOrder } = this.addForm.getRawValue();
    this.categoryCreated.emit({ name, sortOrder });
    this.showAddForm.set(false);
    this.addForm.reset();
  }

  protected startEdit(category: ServiceCategoryResponse): void {
    this.editForm.reset({ name: category.name, sortOrder: category.sortOrder });
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
    const { name, sortOrder } = this.editForm.getRawValue();
    this.categoryUpdated.emit({ id, request: { name, sortOrder } });
    this.editingCategoryId.set(null);
    this.editForm.reset();
  }

  protected onDeleteClicked(id: string): void {
    this.categoryDeleted.emit(id);
  }
}
