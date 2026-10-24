import { Signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged } from 'rxjs';

/** Atraso padrão da busca enquanto o usuário digita. */
export const SEARCH_DEBOUNCE_MS = 300;

/** Versão atrasada de um signal, para buscas com debounce. Chame em contexto de injeção. */
export function debounced<T>(source: Signal<T>, milliseconds = SEARCH_DEBOUNCE_MS): Signal<T> {
  return toSignal(toObservable(source).pipe(debounceTime(milliseconds), distinctUntilChanged()), {
    initialValue: source(),
  });
}
