import { Component, OnInit, OnDestroy, signal, inject, PLATFORM_ID, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { RouterLink } from '@angular/router';
import { 
  LucideShield, 
  LucideLock, 
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
export class PrivacyComponent implements OnInit, OnDestroy {
  private readonly seoService = inject(SeoService);
  private readonly platformId = inject(PLATFORM_ID);
  readonly isBrowser = isPlatformBrowser(this.platformId);

  readonly lastUpdated = 'November 1, 2026';
  readonly activeSection = signal<string>('overview');

  private observer?: IntersectionObserver;
  private scrollListener?: () => void;

  ngOnInit(): void {
    this.seoService.update({
      title: 'Privacy Policy & Cookie Notice',
      description: 'Learn how Movies Center (moviescenter.site) respects user privacy, protects personal account data, uses local storage cookies, and handles analytics securely.',
      keywords: 'moviescenter privacy policy, data protection, cookie policy, gdpr compliance, moviescenter.site privacy',
      url: 'https://moviescenter.site/privacy'
    });

    if (this.isBrowser) {
      this.initScrollSpy();
    }
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
    if (this.scrollListener && this.isBrowser) {
      window.removeEventListener('scroll', this.scrollListener);
    }
  }

  scrollToSection(sectionId: string, event?: Event): void {
    if (event) {
      event.preventDefault();
    }
    this.activeSection.set(sectionId);

    if (!this.isBrowser) return;

    const el = document.getElementById(sectionId);
    if (el) {
      const navOffset = 85;
      const bodyRect = document.body.getBoundingClientRect().top;
      const elementRect = el.getBoundingClientRect().top;
      const offsetPosition = elementRect - bodyRect - navOffset;

      window.scrollTo({
        top: offsetPosition,
        behavior: 'smooth'
      });
    }
  }

  private initScrollSpy(): void {
    const sectionIds = [
      'overview', 
      'information-collected', 
      'cookies-storage', 
      'third-parties', 
      'security', 
      'user-rights', 
      'children', 
      'contact'
    ];

    const observerCallback: IntersectionObserverCallback = (entries) => {
      const intersecting = entries.filter(e => e.isIntersecting);
      if (intersecting.length > 0) {
        intersecting.sort((a, b) => Math.abs(a.boundingClientRect.top - 90) - Math.abs(b.boundingClientRect.top - 90));
        this.activeSection.set(intersecting[0].target.id);
      }
    };

    this.observer = new IntersectionObserver(observerCallback, {
      rootMargin: '-80px 0px -55% 0px',
      threshold: [0, 0.2]
    });

    this.scrollListener = () => {
      if (window.scrollY < 200) {
        this.activeSection.set(sectionIds[0]);
      }
    };
    window.addEventListener('scroll', this.scrollListener, { passive: true });

    setTimeout(() => {
      sectionIds.forEach(id => {
        const el = document.getElementById(id);
        if (el) this.observer?.observe(el);
      });
    }, 150);
  }
}
