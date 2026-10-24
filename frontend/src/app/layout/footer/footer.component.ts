import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideFilm } from '@lucide/angular';
import { APP_NAME } from '../../core/constants/app.constants';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [RouterLink, LucideFilm],
  templateUrl: './footer.component.html',
  styleUrl: './footer.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FooterComponent {
  readonly appName = APP_NAME;
  readonly currentYear = new Date().getFullYear();
}
