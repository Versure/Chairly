import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  inject,
  input,
  InputSignal,
  output,
  OutputEmitterRef,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

import { RecipesApiService } from '../../data-access';
import { Recipe, RecipeProduct } from '../../models';

interface ProductFormGroup {
  name: FormControl<string>;
  brand: FormControl<string>;
  quantity: FormControl<string>;
}

@Component({
  selector: 'chairly-recipe-form',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  templateUrl: './recipe-form.component.html',
})
export class RecipeFormComponent {
  readonly bookingId: InputSignal<string> = input.required<string>();
  readonly existingRecipe: InputSignal<Recipe | null> = input<Recipe | null>(null);

  readonly saved: OutputEmitterRef<Recipe> = output<Recipe>();
  readonly cancelled: OutputEmitterRef<void> = output<void>();

  private readonly document = inject(DOCUMENT);
  private readonly recipesApi = inject(RecipesApiService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly dialogRef = viewChild.required<ElementRef<HTMLDialogElement>>('dialogEl');

  protected readonly isSaving = signal<boolean>(false);
  protected readonly errorMessage = signal<string | null>(null);
  private readonly activeRecipe = signal<Recipe | null>(null);

  protected readonly form = new FormGroup({
    title: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(200)],
    }),
    notes: new FormControl<string>('', {
      nonNullable: true,
      validators: [Validators.maxLength(2000)],
    }),
    products: new FormArray<FormGroup<ProductFormGroup>>([]),
  });

  open(recipe?: Recipe | null): void {
    const recipeData = recipe !== undefined ? recipe : this.existingRecipe();
    this.activeRecipe.set(recipeData ?? null);
    this.errorMessage.set(null);
    this.isSaving.set(false);

    if (recipeData) {
      this.form.controls.title.setValue(recipeData.title);
      this.form.controls.notes.setValue(recipeData.notes ?? '');
      this.form.controls.products.clear();
      for (const product of recipeData.products) {
        this.addProduct(product);
      }
    } else {
      this.form.reset({ title: '', notes: '' });
      this.form.controls.products.clear();
    }

    this.document.body.style.overflow = 'hidden';
    this.dialogRef().nativeElement.showModal();
  }

  close(): void {
    this.document.body.style.overflow = '';
    this.dialogRef().nativeElement.close();
  }

  protected addProduct(product?: RecipeProduct): void {
    const group = new FormGroup<ProductFormGroup>({
      name: new FormControl(product?.name ?? '', {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(100)],
      }),
      brand: new FormControl(product?.brand ?? '', {
        nonNullable: true,
        validators: [Validators.maxLength(100)],
      }),
      quantity: new FormControl(product?.quantity ?? '', {
        nonNullable: true,
        validators: [Validators.maxLength(50)],
      }),
    });
    this.form.controls.products.push(group);
  }

  protected removeProduct(index: number): void {
    this.form.controls.products.removeAt(index);
  }

  protected moveProductUp(index: number): void {
    if (index <= 0) {
      return;
    }
    const controls = this.form.controls.products.controls;
    const current = controls[index];
    this.form.controls.products.removeAt(index);
    this.form.controls.products.insert(index - 1, current);
  }

  protected moveProductDown(index: number): void {
    if (index >= this.form.controls.products.length - 1) {
      return;
    }
    const controls = this.form.controls.products.controls;
    const current = controls[index];
    this.form.controls.products.removeAt(index);
    this.form.controls.products.insert(index + 1, current);
  }

  protected onSave(): void {
    if (this.form.invalid || this.isSaving()) {
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);

    const { title, notes } = this.form.getRawValue();
    const products = this.form.controls.products.controls.map((group, index) => {
      const value = group.getRawValue();
      return {
        name: value.name,
        brand: value.brand || undefined,
        quantity: value.quantity || undefined,
        sortOrder: index,
      };
    });

    const recipeData = this.activeRecipe();
    if (recipeData) {
      this.recipesApi
        .updateRecipe(recipeData.id, {
          title,
          notes: notes || undefined,
          products,
        })
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (updated) => {
            this.isSaving.set(false);
            this.close();
            this.saved.emit(updated);
          },
          error: () => {
            this.isSaving.set(false);
            this.errorMessage.set(
              'Er is een fout opgetreden bij het opslaan. Probeer het opnieuw.',
            );
          },
        });
    } else {
      this.recipesApi
        .createRecipe({
          bookingId: this.bookingId(),
          title,
          notes: notes || undefined,
          products,
        })
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (created) => {
            this.isSaving.set(false);
            this.close();
            this.saved.emit(created);
          },
          error: () => {
            this.isSaving.set(false);
            this.errorMessage.set(
              'Er is een fout opgetreden bij het opslaan. Probeer het opnieuw.',
            );
          },
        });
    }
  }

  protected onCancel(): void {
    this.close();
    this.cancelled.emit();
  }
}
