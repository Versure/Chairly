import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output, OutputEmitterRef } from '@angular/core';

import { DurationPipe, ServiceResponse } from '../util';

@Component({
  selector: 'chairly-service-table',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, DurationPipe],
  template: `
    @if (isLoading()) {
      <div class="flex items-center justify-center py-12 text-sm text-gray-500">
        <span>Loading services...</span>
      </div>
    } @else {
      <div class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200">
          <thead class="bg-gray-50">
            <tr>
              <th class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500">Name</th>
              <th class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500">Category</th>
              <th class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500">Duration</th>
              <th class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500">Price</th>
              <th class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500">Status</th>
              <th class="px-4 py-3 text-right text-xs font-medium uppercase tracking-wider text-gray-500">Actions</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100 bg-white">
            @for (service of services(); track service.id) {
              <tr class="hover:bg-gray-50 transition-colors">
                <td class="px-4 py-3 text-sm font-medium text-gray-900">{{ service.name }}</td>
                <td class="px-4 py-3 text-sm text-gray-600">
                  {{ service.categoryName ?? '—' }}
                </td>
                <td class="px-4 py-3 text-sm text-gray-600">{{ service.duration | duration }}</td>
                <td class="px-4 py-3 text-sm text-gray-600">{{ service.price | currency }}</td>
                <td class="px-4 py-3">
                  @if (service.isActive) {
                    <span class="inline-flex items-center rounded-full bg-green-100 px-2.5 py-0.5 text-xs font-medium text-green-800">
                      Active
                    </span>
                  } @else {
                    <span class="inline-flex items-center rounded-full bg-gray-100 px-2.5 py-0.5 text-xs font-medium text-gray-600">
                      Inactive
                    </span>
                  }
                </td>
                <td class="px-4 py-3 text-right">
                  <div class="flex items-center justify-end gap-2">
                    <button
                      type="button"
                      class="rounded px-2 py-1 text-xs text-gray-500 hover:bg-indigo-50 hover:text-indigo-600"
                      title="Edit service"
                      (click)="editClicked.emit(service)"
                    >
                      Edit
                    </button>
                    <button
                      type="button"
                      class="rounded px-2 py-1 text-xs text-gray-500 hover:bg-yellow-50 hover:text-yellow-600"
                      title="Toggle active"
                      (click)="toggleActiveClicked.emit(service)"
                    >
                      {{ service.isActive ? 'Deactivate' : 'Activate' }}
                    </button>
                    <button
                      type="button"
                      class="rounded px-2 py-1 text-xs text-gray-500 hover:bg-red-50 hover:text-red-600"
                      title="Delete service"
                      (click)="deleteClicked.emit(service)"
                    >
                      Delete
                    </button>
                  </div>
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="6" class="px-4 py-12 text-center text-sm text-gray-400">
                  No services yet. Click "Add Service" to get started.
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    }
  `,
})
export class ServiceTableComponent {
  readonly services = input.required<ServiceResponse[]>();
  readonly isLoading = input.required<boolean>();

  readonly editClicked: OutputEmitterRef<ServiceResponse> = output<ServiceResponse>();
  readonly deleteClicked: OutputEmitterRef<ServiceResponse> = output<ServiceResponse>();
  readonly toggleActiveClicked: OutputEmitterRef<ServiceResponse> = output<ServiceResponse>();
}
