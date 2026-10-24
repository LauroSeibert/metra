import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from './app.routes';
import { AuthService } from './core/auth/auth.service';
import { signInAs, signOut } from './core/auth/testing';
import { LAYOUT_STRINGS } from './core/layout/layout.strings';
import { NAV_ITEMS } from './core/layout/nav-items';
import { HOME_STRINGS } from './features/home/home.strings';

describe('App shell', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting()],
    });
    signInAs();
  });

  afterEach(() => signOut());

  async function render(url: string): Promise<HTMLElement> {
    const harness = await RouterTestingHarness.create(url);
    return harness.fixture.nativeElement as HTMLElement;
  }

  it('renders top bar breadcrumb, side nav and home cards', async () => {
    const root = await render('/');

    expect(root.querySelector('erp-top-bar .breadcrumb li')?.textContent?.trim()).toBe(
      LAYOUT_STRINGS.home,
    );
    expect(root.querySelectorAll('erp-side-nav li').length).toBe(NAV_ITEMS.length);
    expect(root.querySelector('erp-home h1')?.textContent?.trim()).toBe(HOME_STRINGS.title);
    expect(root.querySelectorAll('erp-home erp-card').length).toBe(3);
  });

  it('disables nav items of modules not yet implemented', async () => {
    const root = await render('/');

    const disabled = root.querySelectorAll('erp-side-nav [aria-disabled="true"]');
    expect(disabled.length).toBe(NAV_ITEMS.filter((item) => !item.available).length);
  });

  it('shows the signed-in user in the top bar', async () => {
    const root = await render('/');

    expect(root.querySelector('erp-top-bar .who strong')?.textContent?.trim()).toBe(
      'Usuária Teste',
    );
    expect(root.querySelector('erp-top-bar .avatar')?.textContent?.trim()).toBe('UT');
  });

  it('sends visitors without a session to the login, keeping the requested page', async () => {
    TestBed.inject(AuthService).profile.set(null);
    signOut();
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting()],
    });

    const harness = await RouterTestingHarness.create('/products');

    expect(TestBed.inject(Router).url).toBe('/login?returnUrl=%2Fproducts');
    expect(harness.fixture.nativeElement.querySelector('erp-login')).not.toBeNull();
  });

  it('redirects unknown routes to home', async () => {
    const root = await render('/unknown');

    expect(root.querySelector('erp-home')).not.toBeNull();
  });
});
