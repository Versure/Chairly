import { ChangeDetectionStrategy, Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PageHeaderComponent } from './page-header.component';

@Component({
  selector: 'chairly-page-header-host',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PageHeaderComponent],
  templateUrl: './page-header-host.component.html',
})
class HostWithActionsComponent {}

describe('PageHeaderComponent', () => {
  let component: PageHeaderComponent;
  let fixture: ComponentFixture<PageHeaderComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PageHeaderComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(PageHeaderComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('title', 'Boekingen');
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render the title correctly', () => {
    const h1 = fixture.nativeElement.querySelector('h1') as HTMLHeadingElement;
    expect(h1.textContent).toBe('Boekingen');
  });

  it('should have an empty actions container when no content is projected', () => {
    const actionsContainer = fixture.nativeElement.querySelector(
      'div.flex.items-center.gap-2',
    ) as HTMLDivElement;
    expect(actionsContainer).toBeTruthy();
    expect(actionsContainer.children.length).toBe(0);
  });

  it('should have consistent min-height class', () => {
    const rootDiv = fixture.nativeElement.querySelector('div') as HTMLDivElement;
    expect(rootDiv.classList.contains('min-h-[4rem]')).toBe(true);
  });

  describe('with projected action content', () => {
    let hostFixture: ComponentFixture<HostWithActionsComponent>;

    beforeEach(async () => {
      hostFixture = TestBed.createComponent(HostWithActionsComponent);
      hostFixture.detectChanges();
    });

    it('should render the projected action button', () => {
      const actionsContainer = hostFixture.nativeElement.querySelector(
        'div.flex.items-center.gap-2',
      ) as HTMLDivElement;
      const button = actionsContainer.querySelector('button') as HTMLButtonElement;
      expect(button).toBeTruthy();
      expect(button.textContent?.trim()).toBe('Actie');
    });
  });
});
