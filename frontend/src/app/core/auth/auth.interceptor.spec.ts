import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';
import { signInAs, signOut } from './testing';

describe('authInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    signInAs();
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
  });

  afterEach(() => {
    backend.verify();
    signOut();
  });

  it('sends the access token to the API, but not to login', async () => {
    const products = firstValueFrom(http.get('/api/v1/products'));
    backend.expectOne('/api/v1/products').flush({});
    await products;

    const login = firstValueFrom(http.post('/api/v1/auth/login', {}));
    const loginRequest = backend.expectOne('/api/v1/auth/login');
    expect(loginRequest.request.headers.has('Authorization')).toBe(false);
    loginRequest.flush({});
    await login;
  });

  it('renews the session once on 401 and repeats the call with the new token', async () => {
    const result = firstValueFrom(http.get('/api/v1/products'));

    const first = backend.expectOne('/api/v1/products');
    expect(first.request.headers.get('Authorization')).toBe('Bearer a');
    first.flush(null, { status: 401, statusText: 'Unauthorized' });

    const refresh = await vi.waitFor(() => backend.expectOne('/api/v1/auth/refresh'));
    refresh.flush({ accessToken: 'novo', expiresIn: 900, refreshToken: 'r2' });

    const retry = await vi.waitFor(() => backend.expectOne('/api/v1/products'));
    expect(retry.request.headers.get('Authorization')).toBe('Bearer novo');
    retry.flush({ ok: true });

    expect(await result).toEqual({ ok: true });
  });

  it('ends the session and goes to the login when the refresh is refused', async () => {
    const auth = TestBed.inject(AuthService);
    const result = firstValueFrom(http.get('/api/v1/products')).catch((error: unknown) => error);

    backend.expectOne('/api/v1/products').flush(null, { status: 401, statusText: 'Unauthorized' });
    const refresh = await vi.waitFor(() => backend.expectOne('/api/v1/auth/refresh'));
    refresh.flush(null, { status: 401, statusText: 'Unauthorized' });

    await result;
    expect(auth.isAuthenticated()).toBe(false);
    expect(TestBed.inject(Router).navigate).toHaveBeenCalledWith(['/login'], expect.anything());
  });
});
