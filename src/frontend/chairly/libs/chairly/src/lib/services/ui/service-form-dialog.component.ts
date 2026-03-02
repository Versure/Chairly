import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  input,
  output,
  OutputEmitterRef,
  viewChild,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

import {
  CreateServiceRequest,
  ServiceCategoryResponse,
  ServiceResponse,
  UpdateServiceRequest,
} from '../models';
import { formatDurationToTimeSpan, parseDuration } from '../util';

@Component({
  selector: 'chairly-service-form-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  template: `
    <dialog class="p-0 rounded-lg shadow-xl max-w-lg w-full" #dialogEl>
      <form [formGroup]="form" (ngSubmit)="onSave()">
        <div class="p-6">
          <h2 class="text-lg font-semibold text-gray-900 mb-4">
            {{ service() ? 'Edit Service' : 'Add Service' }}
          </h2>

          <!-- Name -->
          <div class="mb-4">
            <label for="sfd-name" class="block text-sm font-medium text-gray-700 mb-1">Name</label>
            <input
              id="sfd-name"
              formControlName="name"
              type="text"
              class="w-full border border-gray-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-indigo-500"
            />
            @if (form.controls.name.invalid && form.controls.name.touched) {
              <p class="mt-1 text-xs text-red-600">Name is required (max 150 characters).</p>
            }
          </div>

          <!-- Description -->
          <div class="mb-4">
            <label for="sfd-description" class="block text-sm font-medium text-gray-700 mb-1">Description</label>
            <textarea
              id="sfd-description"
              formControlName="description"
              rows="3"
              class="w-full border border-gray-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-indigo-500"
            ></textarea>
            @if (form.controls.description.invalid && form.controls.description.touched) {
              <p class="mt-1 text-xs text-red-600">Description must be at most 2000 characters.</p>
            }
          </div>

          <!-- Duration and Price -->
          <div class="mb-4 grid grid-cols-2 gap-4">
            <div>
              <label for="sfd-duration" class="block text-sm font-medium text-gray-700 mb-1">Duration (minutes)</label>
              <input
                id="sfd-duration"
                formControlName="duration"
                type="number"
                min="1"
                class="w-full border border-gray-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-indigo-500"
              />
              @if (form.controls.duration.invalid && form.controls.duration.touched) {
                <p class="mt-1 text-xs text-red-600">Duration is required.</p>
              }
            </div>
            <div>
              <label for="sfd-price" class="block text-sm font-medium text-gray-700 mb-1">Price</label>
              <input
                id="sfd-price"
                formControlName="price"
                type="number"
                min="0"
                step="0.01"
                class="w-full border border-gray-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-indigo-500"
              />
              @if (form.controls.price.invalid && form.controls.price.touched) {
                <p class="mt-1 text-xs text-red-600">Price is required and must be 0 or more.</p>
              }
            </div>
          </div>

          <!-- Category and Sort Order -->
          <div class="mb-6 grid grid-cols-2 gap-4">
            <div>
              <label for="sfd-categoryId" class="block text-sm font-medium text-gray-700 mb-1">Category</label>
              <select
                id="sfd-categoryId"
                formControlName="categoryId"
                class="w-full border border-gray-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-indigo-500"
              >
                <option [value]="null">None</option>
                @for (category of categories(); track category.id) {
                  <option [value]="category.id">{{ category.name }}</option>
                }
              </select>
            </div>
            <div>
              <label for="sfd-sortOrder" class="block text-sm font-medium text-gray-700 mb-1">Sort Order</label>
              <input
                id="sfd-sortOrder"
                formControlName="sortOrder"
                type="number"
                class="w-full border border-gray-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-indigo-500"
              />
            </div>
          </div>

          <!-- Actions -->
          <div class="flex justify-end gap-3">
            <button
              type="button"
              class="rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2"
              (click)="onCancel()"
            >
              Cancel
            </button>
            <button
              type="submit"
              [disabled]="form.invalid"
              class="rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
            >
              Save
            </button>
          </div>
        </div>
      </form>
    </dialog>
  `,
})
export class ServiceFormDialogComponent {
  readonly categories = input.required<ServiceCategoryResponse[]>();
  readonly service = input<ServiceResponse | null>(null);

  readonly saved: OutputEmitterRef<CreateServiceRequest | UpdateServiceRequest> =
    output<CreateServiceRequest | UpdateServiceRequest>();
  readonly cancelled: OutputEmitterRef<void> = output<void>();

  private readonly dialogRef =
    viewChild.required<ElementRef<HTMLDialogElement>>('dialogEl');

  protected readonly form = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(150)],
    }),
    description: new FormControl<string | null>(null, {
      validators: [Validators.maxLength(2000)],
    }),
    duration: new FormControl<number>(30, {
      nonNullable: true,
      validators: [Validators.required],
    }),
    price: new FormControl<number>(0, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(0)],
    }),
    categoryId: new FormControl<string | null>(null),
    sortOrder: new FormControl<number>(0, { nonNullable: true }),
  });

  open(): void {
    const svc = this.service();
    if (svc) {
      this.form.reset({
        name: svc.name,
        description: svc.description,
        duration: parseDuration(svc.duration),
        price: svc.price,
        categoryId: svc.categoryId,
        sortOrder: svc.sortOrder,
      });
    } else {
      this.form.reset({
        name: '',
        description: null,
        duration: 30,
        price: 0,
        categoryId: null,
        sortOrder: 0,
      });
    }
    this.dialogRef().nativeElement.showModal();
  }

  close(): void {
    this.dialogRef().nativeElement.close();
  }

  protected onSave(): void {
    if (this.form.invalid) {
      return;
    }
    const { name, description, duration, price, categoryId, sortOrder } =
      this.form.getRawValue();
    this.close();
    this.saved.emit({
      name,
      description,
      duration: formatDurationToTimeSpan(duration),
      price,
      categoryId,
      sortOrder,
    });
  }

  protected onCancel(): void {
    this.close();
    this.cancelled.emit();
  }
}
