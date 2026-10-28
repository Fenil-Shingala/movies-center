import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from '../header/header.component';
import { FooterComponent } from '../footer/footer.component';
import { MobileTabBarComponent } from '../mobile-tab-bar/mobile-tab-bar.component';
import { CookieNoticeComponent } from '../cookie-notice/cookie-notice.component';
import { AuthModalComponent } from '../../shared/components/auth-modal/auth-modal.component';
import { PwaPromptComponent } from '../../shared/components/pwa-prompt/pwa-prompt.component';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    RouterOutlet, 
    HeaderComponent, 
    FooterComponent, 
    MobileTabBarComponent, 
    CookieNoticeComponent, 
    AuthModalComponent,
    PwaPromptComponent
  ],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ShellComponent {}
