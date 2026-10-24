import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { Login } from './login';

describe('Login', () => {
  let fixture: ComponentFixture<Login>;
  let element: HTMLElement;
  let auth: { login: ReturnType<typeof vi.fn> };
  let router: Router;

  async function render(inputs: { returnUrl?: string; expired?: string } = {}): Promise<void> {
    auth = { login: vi.fn().mockResolvedValue(undefined) };
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: AuthService, useValue: auth }],
    });
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    fixture = TestBed.createComponent(Login);
    for (const [name, value] of Object.entries(inputs)) {
      fixture.componentRef.setInput(name, value);
    }
    element = fixture.nativeElement as HTMLElement;
    await fixture.whenStable();
    fixture.detectChanges();
  }

  async function fillAndSubmit(email: string, password: string): Promise<void> {
    const inputs = element.querySelectorAll<HTMLInputElement>('input');
    inputs[0].value = email;
    inputs[0].dispatchEvent(new Event('input'));
    inputs[1].value = password;
    inputs[1].dispatchEvent(new Event('input'));
    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('signs in and returns to the requested page', async () => {
    await render({ returnUrl: '/partners?page=2' });

    await fillAndSubmit(' maria@erp.local ', 'Segredo123');

    expect(auth.login).toHaveBeenCalledWith('maria@erp.local', 'Segredo123');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/partners?page=2');
  });

  it('ignores a returnUrl that points outside the app', async () => {
    await render({ returnUrl: '//evil.example/phish' });

    await fillAndSubmit('maria@erp.local', 'Segredo123');

    expect(router.navigateByUrl).toHaveBeenCalledWith('/');
  });

  it('shows the server message when the credentials are refused', async () => {
    await render();
    auth.login.mockRejectedValue(
      new HttpErrorResponse({
        status: 401,
        error: { code: 'auth.locked_out', detail: 'Conta bloqueada por excesso de tentativas.' },
      }),
    );

    await fillAndSubmit('maria@erp.local', 'errada');

    expect(element.querySelector('.notice.error')?.textContent).toContain('Conta bloqueada');
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('validates the fields before calling the API', async () => {
    await render();

    await fillAndSubmit('sem-arroba', '');

    expect(auth.login).not.toHaveBeenCalled();
  });
});
