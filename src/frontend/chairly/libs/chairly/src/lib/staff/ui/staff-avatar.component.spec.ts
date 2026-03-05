import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StaffAvatarComponent } from './staff-avatar.component';

describe('StaffAvatarComponent', () => {
  let component: StaffAvatarComponent;
  let fixture: ComponentFixture<StaffAvatarComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StaffAvatarComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(StaffAvatarComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('color', '#6366f1');
    fixture.componentRef.setInput('initials', 'JD');
    fixture.componentRef.setInput('photoUrl', null);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render initials when photoUrl is null', () => {
    const div = fixture.nativeElement.querySelector('div') as HTMLDivElement;
    expect(div).toBeTruthy();
    expect(div.textContent?.trim()).toBe('JD');
  });

  it('should apply background-color from color input when no photoUrl', () => {
    const div = fixture.nativeElement.querySelector('div') as HTMLDivElement;
    expect(div.style.backgroundColor).toBeTruthy();
  });

  it('should render img when photoUrl is provided', () => {
    fixture.componentRef.setInput('photoUrl', 'https://example.com/photo.jpg');
    fixture.detectChanges();

    const img = fixture.nativeElement.querySelector('img') as HTMLImageElement;
    expect(img).toBeTruthy();
    expect(img.src).toContain('https://example.com/photo.jpg');
  });

  it('should not render initials div when photoUrl is provided', () => {
    fixture.componentRef.setInput('photoUrl', 'https://example.com/photo.jpg');
    fixture.detectChanges();

    const div = fixture.nativeElement.querySelector('div');
    expect(div).toBeNull();
  });

  it('should apply size class sm (w-8 h-8)', () => {
    fixture.componentRef.setInput('size', 'sm');
    fixture.detectChanges();

    const div = fixture.nativeElement.querySelector('div') as HTMLDivElement;
    expect(div.className).toContain('w-8');
    expect(div.className).toContain('h-8');
  });

  it('should apply size class md (w-10 h-10) by default', () => {
    const div = fixture.nativeElement.querySelector('div') as HTMLDivElement;
    expect(div.className).toContain('w-10');
    expect(div.className).toContain('h-10');
  });

  it('should apply size class lg (w-14 h-14)', () => {
    fixture.componentRef.setInput('size', 'lg');
    fixture.detectChanges();

    const div = fixture.nativeElement.querySelector('div') as HTMLDivElement;
    expect(div.className).toContain('w-14');
    expect(div.className).toContain('h-14');
  });
});
