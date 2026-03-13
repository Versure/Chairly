import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { ShellComponent } from './shell.component';

describe('ShellComponent', () => {
  let component: ShellComponent;
  let fixture: ComponentFixture<ShellComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ShellComponent],
      providers: [provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(ShellComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('sidebarOpen signal defaults to false', () => {
    expect(component.sidebarOpen()).toBe(false);
  });

  it('toggleSidebar() flips the value', () => {
    component.toggleSidebar();
    expect(component.sidebarOpen()).toBe(true);

    component.toggleSidebar();
    expect(component.sidebarOpen()).toBe(false);
  });

  it('closeSidebar() sets to false', () => {
    component.toggleSidebar();
    expect(component.sidebarOpen()).toBe(true);

    component.closeSidebar();
    expect(component.sidebarOpen()).toBe(false);
  });

  it('closeSidebar() is idempotent when already closed', () => {
    expect(component.sidebarOpen()).toBe(false);

    component.closeSidebar();
    expect(component.sidebarOpen()).toBe(false);
  });

  it('all nav items render an SVG icon', () => {
    const navItems = fixture.nativeElement.querySelectorAll('nav ul li a');
    expect(navItems.length).toBe(7);

    navItems.forEach((link: HTMLElement) => {
      const svg = link.querySelector('svg');
      expect(svg).toBeTruthy();
      expect(svg?.getAttribute('aria-hidden')).toBe('true');
    });
  });
});
