import { CurrencyPipe, DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';

import { PageHeaderComponent, paymentMethodLabels } from '@org/shared-lib';

import { ReportStore } from '../../data-access';
import { PeriodType, RevenueReportDailyTotal, RevenueReportRow } from '../../models';

interface DayGroup {
  date: string;
  rows: RevenueReportRow[];
  total: RevenueReportDailyTotal | null;
}

@Component({
  selector: 'chairly-revenue-report-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, DatePipe, PageHeaderComponent],
  templateUrl: './revenue-report-page.component.html',
  styleUrl: './revenue-report-page.component.scss',
})
export class RevenueReportPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly reportStore = inject(ReportStore);

  protected readonly paymentMethodLabels = paymentMethodLabels;
  protected readonly selectedPeriod = signal<PeriodType>('week');
  protected readonly selectedDate = signal<string>(new Date().toISOString().split('T')[0]);

  protected readonly report = computed(() => this.reportStore.report());
  protected readonly isLoading = computed(() => this.reportStore.isLoading());
  protected readonly isDownloading = computed(() => this.reportStore.isDownloading());

  protected readonly periodLabel = computed<string>(() => {
    const report = this.report();
    if (!report) {
      return '';
    }
    if (report.periodType === 'week') {
      const start = new Date(report.periodStart);
      const weekNumber = this.getIsoWeekNumber(start);
      const startStr = this.formatShortDate(report.periodStart);
      const endStr = this.formatShortDate(report.periodEnd);
      const year = start.getFullYear();
      return `Week ${weekNumber}: ${startStr} - ${endStr} ${year}`;
    }
    return this.formatMonthYear(report.periodStart);
  });

  protected readonly dayGroups = computed<DayGroup[]>(() => {
    const report = this.report();
    if (!report) {
      return [];
    }
    const groupMap = new Map<string, RevenueReportRow[]>();
    for (const row of report.rows) {
      const existing = groupMap.get(row.date);
      if (existing) {
        existing.push(row);
      } else {
        groupMap.set(row.date, [row]);
      }
    }
    const groups: DayGroup[] = [];
    for (const [date, rows] of groupMap) {
      const total = report.dailyTotals.find((dt) => dt.date === date) ?? null;
      groups.push({ date, rows, total });
    }
    return groups;
  });

  ngOnInit(): void {
    this.route.queryParams.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const periode = (params['periode'] as PeriodType) ?? 'week';
      const datum = (params['datum'] as string) ?? new Date().toISOString().split('T')[0];
      this.selectedPeriod.set(periode);
      this.selectedDate.set(datum);
      this.reportStore.loadReport(periode, datum);
    });
  }

  protected onPeriodChange(period: PeriodType): void {
    this.router.navigate([], {
      queryParams: { periode: period, datum: this.selectedDate() },
      queryParamsHandling: 'merge',
    });
  }

  protected onPrevPeriod(): void {
    const date = new Date(this.selectedDate());
    if (this.selectedPeriod() === 'week') {
      date.setDate(date.getDate() - 7);
    } else {
      date.setMonth(date.getMonth() - 1);
    }
    this.router.navigate([], {
      queryParams: { periode: this.selectedPeriod(), datum: this.toDateString(date) },
      queryParamsHandling: 'merge',
    });
  }

  protected onNextPeriod(): void {
    const date = new Date(this.selectedDate());
    if (this.selectedPeriod() === 'week') {
      date.setDate(date.getDate() + 7);
    } else {
      date.setMonth(date.getMonth() + 1);
    }
    this.router.navigate([], {
      queryParams: { periode: this.selectedPeriod(), datum: this.toDateString(date) },
      queryParamsHandling: 'merge',
    });
  }

  protected onDownloadPdf(): void {
    this.reportStore.downloadPdf(this.selectedPeriod(), this.selectedDate());
  }

  private getIsoWeekNumber(date: Date): number {
    const d = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
    const dayNum = d.getUTCDay() || 7;
    d.setUTCDate(d.getUTCDate() + 4 - dayNum);
    const yearStart = new Date(Date.UTC(d.getUTCFullYear(), 0, 1));
    return Math.ceil(((d.getTime() - yearStart.getTime()) / 86400000 + 1) / 7);
  }

  private formatShortDate(dateStr: string): string {
    const months = [
      'jan',
      'feb',
      'mrt',
      'apr',
      'mei',
      'jun',
      'jul',
      'aug',
      'sep',
      'okt',
      'nov',
      'dec',
    ];
    const d = new Date(dateStr);
    return `${d.getDate()} ${months[d.getMonth()]}`;
  }

  private formatMonthYear(dateStr: string): string {
    const months = [
      'Januari',
      'Februari',
      'Maart',
      'April',
      'Mei',
      'Juni',
      'Juli',
      'Augustus',
      'September',
      'Oktober',
      'November',
      'December',
    ];
    const d = new Date(dateStr);
    return `${months[d.getMonth()]} ${d.getFullYear()}`;
  }

  private toDateString(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
