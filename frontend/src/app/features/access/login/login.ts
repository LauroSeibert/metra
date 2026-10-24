import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { email, form, FormField, required, schema, submit } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { toApiProblem } from '../../../core/http/api-problem';
import { LAYOUT_STRINGS } from '../../../core/layout/layout.strings';
import { ACCESS_STRINGS } from '../access.strings';

interface LoginModel {
  email: string;
  password: string;
}

const messages = ACCESS_STRINGS.validation;
const loginSchema = schema<LoginModel>((path) => {
  required(path.email, { message: messages.required });
  email(path.email, { message: messages.email });
  required(path.password, { message: messages.required });
});

/** Tela de login, fora do layout principal. Depois de entrar, volta para `returnUrl`. */
@Component({
  selector: 'erp-login',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormField, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  /** Query params (withComponentInputBinding). */
  readonly returnUrl = input<string>();
  readonly expired = input<string>();

  protected readonly strings = ACCESS_STRINGS.login;
  protected readonly app = LAYOUT_STRINGS;
  protected readonly model = signal<LoginModel>({ email: '', password: '' });
  protected readonly loginForm = form(this.model, loginSchema);
  protected readonly showPassword = signal(false);
  protected readonly error = computed(() => this.loginForm().errors()[0]?.message ?? null);

  protected async signIn(event: Event): Promise<void> {
    event.preventDefault();
    await submit(this.loginForm, async () => {
      try {
        const { email: address, password } = this.model();
        await this.auth.login(address.trim(), password);
        await this.router.navigateByUrl(this.safeReturnUrl());
        return undefined;
      } catch (error) {
        // Erro geral (sem campo): e-mail ou senha errados, conta bloqueada, muitas tentativas.
        return [{ kind: 'server', message: toApiProblem(error).message }];
      }
    });
  }

  /** Só caminhos internos: um returnUrl externo na URL não pode levar o usuário para fora. */
  private safeReturnUrl(): string {
    const url = this.returnUrl();
    return url && url.startsWith('/') && !url.startsWith('//') ? url : '/';
  }
}
