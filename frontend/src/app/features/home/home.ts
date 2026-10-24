import { ChangeDetectionStrategy, Component } from '@angular/core';
import { Card } from '../../shared/ui/card/card';
import { HOME_STRINGS } from './home.strings';

@Component({
  selector: 'erp-home',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Card],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home {
  protected readonly strings = HOME_STRINGS;
}
