import { ChangeDetectionStrategy, Component } from '@angular/core';

/** Cartão branco de cantos arredondados, base dos resumos e das tabelas. */
@Component({
  selector: 'erp-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content />',
  styleUrl: './card.scss',
})
export class Card {}
