import { ChangeDetectionStrategy, Component, computed, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';

import { LoadingIndicatorComponent, TemplateTypeLabelPipe } from '@org/shared-lib';

import { EmailTemplateApiService, EmailTemplateStore } from '../../data-access';
import { EmailTemplateResponse } from '../../models';

@Component({
  selector: 'chairly-email-templates-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, LoadingIndicatorComponent, TemplateTypeLabelPipe],
  providers: [EmailTemplateStore, EmailTemplateApiService],
  templateUrl: './email-templates-page.component.html',
})
export class EmailTemplatesPageComponent implements OnInit {
  private readonly store = inject(EmailTemplateStore);

  protected readonly templates = computed<EmailTemplateResponse[]>(() => this.store.templates());
  protected readonly isLoading = computed<boolean>(() => this.store.isLoading());

  ngOnInit(): void {
    this.store.loadTemplates();
  }
}
