import { Component, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { 
  LucideShield, 
  LucideLock, 
  LucideDatabase, 
  LucideEye, 
  LucideCookie, 
  LucideMail, 
  LucideClock, 
  LucideCheckCircle2,
  LucideGlobe,
  LucideScale
} from '@lucide/angular';
import { SeoService } from '../../core/services/seo.service';

@Component({
  selector: 'app-privacy',
  standalone: true,
  imports: [
    CommonModule, 
    RouterLink,
    LucideShield, 
    LucideLock, 
    LucideDatabase, 
    LucideEye, 
    LucideCookie, 
    LucideMail, 
    LucideClock, 
    LucideCheckCircle2,
    LucideGlobe,
    LucideScale
  ],
  templateUrl: './privacy.component.html',
  styleUrl: './privacy.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PrivacyComponent implements OnInit {
  private readonly seoService = inject(SeoService);

  readonly lastUpdated = 'November 1, 2026';

  ngOnInit(): void {
    this.seoService.update({
      title: 'Privacy Policy & Cookie Notice',
      description: 'Learn how Movies Center (moviescenter.site) respects user privacy, protects personal account data, uses local storage cookies, and handles analytics securely.',
      keywords: 'moviescenter privacy policy, data protection, cookie policy, gdpr compliance, moviescenter.site privacy',
      url: 'https://moviescenter.site/privacy'
    });
  }
}
