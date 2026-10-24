import { DestroyRef, effect, inject, Signal, signal } from '@angular/core';

/**
 * Tempo mínimo de carregamento antes de mostrar skeleton ou barra de progresso. Respostas rápidas
 * (a maioria, com a API local) trocam direto para os dados, sem piscar o estado de carregando.
 */
export const LOADING_DELAY_MS = 250;

/**
 * Versão atrasada de um sinal booleano: fica `true` só depois de a origem ficar `true` por
 * `milliseconds` seguidos, e volta a `false` na hora. Chame em contexto de injeção.
 */
export function delayedFlag(
  source: Signal<boolean>,
  milliseconds = LOADING_DELAY_MS,
): Signal<boolean> {
  const visible = signal(false);
  let timer: ReturnType<typeof setTimeout> | undefined;
  const cancel = () => {
    clearTimeout(timer);
    timer = undefined;
  };

  effect(() => {
    if (!source()) {
      cancel();
      visible.set(false);
    } else if (timer === undefined) {
      timer = setTimeout(() => {
        timer = undefined;
        visible.set(true);
      }, milliseconds);
    }
  });
  inject(DestroyRef).onDestroy(cancel);

  return visible.asReadonly();
}
