import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { createPartner } from '../../../api/fn/partners/create-partner';
import { getPartner } from '../../../api/fn/partners/get-partner';
import { PartnerDto } from '../../../api/models/partner-dto';
import { Role } from '../../../core/auth/auth.service';
import { signInAs, signOut } from '../../../core/auth/testing';
import { FakeApi } from '../../../shared/testing/fake-api';
import { PARTNERS_STRINGS } from '../partners.strings';
import { PartnerForm, PartnerFormModel } from './partner-form';

const EXISTING: PartnerDto = {
  id: 5,
  type: 'Supplier',
  name: 'Papelaria Central',
  document: '11222333000181',
  documentKind: 'Cnpj',
  email: 'contato@central.com.br',
  phone: null,
  active: true,
  version: 2,
  createdAt: '2026-10-24T12:00:00Z',
  updatedAt: '2026-10-24T12:00:00Z',
};

describe('PartnerForm', () => {
  let api: FakeApi;
  let fixture: ComponentFixture<PartnerForm>;
  let element: HTMLElement;

  async function render(id?: string, role: Role = 'Operator'): Promise<void> {
    TestBed.configureTestingModule({ providers: [api.provide(), provideRouter([])] });
    signInAs(role);
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture = TestBed.createComponent(PartnerForm);
    if (id) {
      fixture.componentRef.setInput('id', id);
    }
    element = fixture.nativeElement as HTMLElement;
    await settle();
  }

  async function settle(): Promise<void> {
    await fixture.whenStable();
    fixture.detectChanges();
  }

  function model(): { set(value: PartnerFormModel): void; (): PartnerFormModel } {
    return (fixture.componentInstance as unknown as { model: never }).model;
  }

  async function submit(): Promise<void> {
    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    await settle();
    await settle();
  }

  beforeEach(() => {
    api = new FakeApi();
  });

  afterEach(() => signOut());

  it('blocks an invalid CPF before calling the API', async () => {
    await render();
    model().set({
      type: 'Customer',
      name: 'Maria',
      document: '529.982.247-24',
      email: '',
      phone: '',
    });

    await submit();

    expect(api.callsTo(createPartner)).toEqual([]);
    expect(element.textContent).toContain(PARTNERS_STRINGS.validation.document);
  });

  it('creates the partner and opens its edit URL', async () => {
    api.on(createPartner, () => ({ ...EXISTING, id: 9 }));
    await render();
    model().set({
      type: 'Customer',
      name: '  Maria Silva ',
      document: '529.982.247-25',
      email: 'maria@exemplo.com',
      phone: '',
    });

    await submit();

    expect(api.callsTo(createPartner)).toEqual([
      {
        body: {
          type: 'Customer',
          name: 'Maria Silva',
          document: '529.982.247-25',
          email: 'maria@exemplo.com',
          phone: null,
        },
      },
    ]);
    expect(TestBed.inject(Router).navigate).toHaveBeenCalledWith(['/partners', 'edit', 9], {
      replaceUrl: true,
    });
  });

  it('shows the document formatted when editing', async () => {
    api.on(getPartner, () => EXISTING);
    await render('5');

    expect(model()().document).toBe('11.222.333/0001-81');
  });

  it('is read-only for the Viewer profile', async () => {
    api.on(getPartner, () => EXISTING);
    await render('5', 'Viewer');

    expect(element.querySelector('button[type="submit"]')).toBeNull();
    expect(element.querySelector<HTMLInputElement>('input')?.disabled).toBe(true);
    expect(element.textContent).toContain(PARTNERS_STRINGS.form.readOnly);
  });
});
