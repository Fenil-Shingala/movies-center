import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { LucideHome, LucideSearch, LucideFilm, LucideTv, LucideUser } from '@lucide/angular';

@Component({
  selector: 'app-mobile-tab-bar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, LucideHome, LucideSearch, LucideFilm, LucideTv, LucideUser],
  templateUrl: './mobile-tab-bar.component.html',
  styleUrl: './mobile-tab-bar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MobileTabBarComponent {}
