import { CurrencyPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
  OutputEmitterRef,
  signal,
} from '@angular/core';

import { ServiceResponse } from '../models';
import { DurationPipe } from '../pipes';

@Component({
  selector: 'chairly-service-table',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, DurationPipe],
  templateUrl: './service-table.component.html',
})
export class ServiceTableComponent {
  readonly services = input.required<ServiceResponse[]>();
  readonly isLoading = input.required<boolean>();

  readonly editClicked: OutputEmitterRef<ServiceResponse> = output<ServiceResponse>();
  readonly deleteClicked: OutputEmitterRef<ServiceResponse> = output<ServiceResponse>();
  readonly toggleActiveClicked: OutputEmitterRef<ServiceResponse> = output<ServiceResponse>();
  readonly servicesReordered: OutputEmitterRef<ServiceResponse[]> = output<ServiceResponse[]>();

  protected readonly draggedIndex = signal<number | null>(null);

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
    const ordered = [...this.services()];
    const [removed] = ordered.splice(fromIndex, 1);
    ordered.splice(dropIndex, 0, removed);
    this.draggedIndex.set(null);
    this.servicesReordered.emit(ordered);
  }
}
