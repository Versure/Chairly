import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BookingStatus } from '../../models';
import { TimelineStatusFilterComponent } from './timeline-status-filter.component';

@Component({
  standalone: true,
  imports: [TimelineStatusFilterComponent],
  template: `<chairly-timeline-status-filter [(value)]="statusFilter" [counts]="statusCounts()" />`,
})
class TestHostComponent {
  readonly statusFilter = signal<BookingStatus | 'All'>('All');
  readonly statusCounts = signal<Record<BookingStatus | 'All', number>>({
    All: 10,
    Scheduled: 2,
    Confirmed: 1,
    InProgress: 0,
    Completed: 5,
    Cancelled: 1,
    NoShow: 1,
  });
}

describe('TimelineStatusFilterComponent', () => {
  let fixture: ComponentFixture<TestHostComponent>;
  let host: TestHostComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHostComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should render five chips with Dutch labels', () => {
    const buttons = fixture.nativeElement.querySelectorAll('button');
    expect(buttons.length).toBe(5);

    const labels = Array.from(buttons).map((b: HTMLButtonElement) => b.textContent?.trim() ?? '');
    expect(labels[0]).toContain('Alle');
    expect(labels[1]).toContain('Voltooid');
    expect(labels[2]).toContain('Geannuleerd');
    expect(labels[3]).toContain('No-show');
    expect(labels[4]).toContain('Gepland');
  });

  it('should show counts next to labels', () => {
    const buttons = fixture.nativeElement.querySelectorAll('button');
    const labels = Array.from(buttons).map((b: HTMLButtonElement) => b.textContent?.trim() ?? '');
    expect(labels[0]).toContain('(10)');
    expect(labels[1]).toContain('(5)');
    expect(labels[4]).toContain('(3)'); // Gepland = Scheduled(2) + Confirmed(1) + InProgress(0)
  });

  it('should update the model value when a chip is clicked', () => {
    const buttons = fixture.nativeElement.querySelectorAll('button');
    buttons[1].click(); // Voltooid
    fixture.detectChanges();

    expect(host.statusFilter()).toBe('Completed');
  });

  it('should mark the active chip as selected via aria-pressed', () => {
    const buttons = fixture.nativeElement.querySelectorAll('button');
    expect(buttons[0].getAttribute('aria-pressed')).toBe('true'); // Alle is default
    expect(buttons[1].getAttribute('aria-pressed')).toBe('false');
  });
});
