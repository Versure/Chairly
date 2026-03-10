import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  input,
  InputSignal,
  output,
  OutputEmitterRef,
  signal,
} from '@angular/core';

import { ClientRecipeSummary } from '../../models';

@Component({
  selector: 'chairly-client-recipe-history',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe],
  templateUrl: './client-recipe-history.component.html',
})
export class ClientRecipeHistoryComponent {
  readonly recipes: InputSignal<ClientRecipeSummary[]> = input.required<ClientRecipeSummary[]>();
  readonly isLoading: InputSignal<boolean> = input<boolean>(false);
  readonly canEdit: InputSignal<boolean> = input<boolean>(false);

  readonly editRecipe: OutputEmitterRef<ClientRecipeSummary> = output<ClientRecipeSummary>();

  protected readonly expandedRecipeId = signal<string | null>(null);

  protected toggleNotes(recipeId: string): void {
    const current = this.expandedRecipeId();
    this.expandedRecipeId.set(current === recipeId ? null : recipeId);
  }
}
