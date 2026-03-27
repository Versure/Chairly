import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

// eslint-disable-next-line sonarjs/deprecation -- KeycloakService is required for runtime config
import { KeycloakService } from 'keycloak-angular';

import { AuthStore } from '../../data-access';
import { ShellComponent } from './shell.component';

function createKeycloakMock(
  firstName: string,
  lastName: string,
  roles: string[],
): Record<string, ReturnType<typeof vi.fn>> {
  return {
    loadUserProfile: vi.fn().mockResolvedValue({ firstName, lastName }),
    getUserRoles: vi.fn().mockReturnValue(roles),
    logout: vi.fn().mockResolvedValue(undefined),
  };
}

async function setupTestBed(
  keycloakServiceMock: Record<string, ReturnType<typeof vi.fn>>,
): Promise<{ fixture: ComponentFixture<ShellComponent>; component: ShellComponent }> {
  await TestBed.configureTestingModule({
    imports: [ShellComponent],
    providers: [
      provideRouter([]),
      AuthStore,
      // eslint-disable-next-line sonarjs/deprecation -- KeycloakService mock for testing
      { provide: KeycloakService, useValue: keycloakServiceMock },
    ],
  }).compileComponents();

  const fixture = TestBed.createComponent(ShellComponent);
  const component = fixture.componentInstance;
  return { fixture, component };
}

describe('ShellComponent', () => {
  let component: ShellComponent;
  let fixture: ComponentFixture<ShellComponent>;

  beforeEach(async () => {
    const keycloakServiceMock = createKeycloakMock('Test', 'User', ['owner']);
    const result = await setupTestBed(keycloakServiceMock);
    fixture = result.fixture;
    component = result.component;
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
});

describe('ShellComponent auth behavior', () => {
  it('displays user full name from AuthStore', async () => {
    const keycloakServiceMock = createKeycloakMock('Jan', 'de Vries', ['owner']);
    const { fixture } = await setupTestBed(keycloakServiceMock);
    fixture.detectChanges();

    const authStore = TestBed.inject(AuthStore);
    authStore.loadUserProfile();
    await fixture.whenStable();
    fixture.detectChanges();

    const nativeElement: HTMLElement = fixture.nativeElement;
    const userNameElement = nativeElement.querySelector('.border-t.border-primary-600 .truncate');
    expect(userNameElement?.textContent?.trim()).toBe('Jan de Vries');
  });

  it('clicking "Uitloggen" calls authStore.logout()', async () => {
    const keycloakServiceMock = createKeycloakMock('Test', 'User', ['owner']);
    const { fixture } = await setupTestBed(keycloakServiceMock);
    fixture.detectChanges();

    const nativeElement: HTMLElement = fixture.nativeElement;
    const logoutButton = Array.from(nativeElement.querySelectorAll('button')).find((btn) =>
      btn.textContent?.trim().includes('Uitloggen'),
    );

    expect(logoutButton).toBeTruthy();
    logoutButton?.click();

    expect(keycloakServiceMock['logout']).toHaveBeenCalled();
  });

  it('hides "Facturen" nav item when user role is staff_member', async () => {
    const keycloakServiceMock = createKeycloakMock('Staff', 'Member', ['staff_member']);
    const { fixture } = await setupTestBed(keycloakServiceMock);
    fixture.detectChanges();

    const authStore = TestBed.inject(AuthStore);
    authStore.loadUserProfile();
    await fixture.whenStable();
    fixture.detectChanges();

    const nativeElement: HTMLElement = fixture.nativeElement;
    const navLinks = Array.from(nativeElement.querySelectorAll('a'));
    const facturenLink = navLinks.find((a) => a.textContent?.trim().includes('Facturen'));

    expect(facturenLink).toBeUndefined();
  });

  it('shows "Instellingen" nav item when user role is manager', async () => {
    const keycloakServiceMock = createKeycloakMock('Manager', 'User', ['manager']);
    const { fixture } = await setupTestBed(keycloakServiceMock);
    fixture.detectChanges();

    const authStore = TestBed.inject(AuthStore);
    authStore.loadUserProfile();
    await fixture.whenStable();
    fixture.detectChanges();

    const nativeElement: HTMLElement = fixture.nativeElement;
    const navLinks = Array.from(nativeElement.querySelectorAll('a'));
    const instellingenLink = navLinks.find((a) => a.textContent?.trim().includes('Instellingen'));

    expect(instellingenLink).toBeTruthy();
  });

  it('shows "Instellingen" nav item when user role is owner', async () => {
    const keycloakServiceMock = createKeycloakMock('Owner', 'User', ['owner']);
    const { fixture } = await setupTestBed(keycloakServiceMock);
    fixture.detectChanges();

    const authStore = TestBed.inject(AuthStore);
    authStore.loadUserProfile();
    await fixture.whenStable();
    fixture.detectChanges();

    const nativeElement: HTMLElement = fixture.nativeElement;
    const navLinks = Array.from(nativeElement.querySelectorAll('a'));
    const instellingenLink = navLinks.find((a) => a.textContent?.trim().includes('Instellingen'));

    expect(instellingenLink).toBeTruthy();
  });

  it('hides "Instellingen" nav item when user role is staff_member', async () => {
    const keycloakServiceMock = createKeycloakMock('Staff', 'Member', ['staff_member']);
    const { fixture } = await setupTestBed(keycloakServiceMock);
    fixture.detectChanges();

    const authStore = TestBed.inject(AuthStore);
    authStore.loadUserProfile();
    await fixture.whenStable();
    fixture.detectChanges();

    const nativeElement: HTMLElement = fixture.nativeElement;
    const navLinks = Array.from(nativeElement.querySelectorAll('a'));
    const instellingenLink = navLinks.find((a) => a.textContent?.trim().includes('Instellingen'));

    expect(instellingenLink).toBeUndefined();
  });
});
