import { Component, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { 
  LucideShield, 
  LucideFileText, 
  LucideAlertTriangle, 
  LucideScale, 
  LucideExternalLink, 
  LucideMail, 
  LucideClock, 
  LucideCheckCircle2,
  LucideGlobe
} from '@lucide/angular';
import { SeoService } from '../../core/services/seo.service';

@Component({
  selector: 'app-terms',
  standalone: true,
  imports: [
    CommonModule, 
    RouterLink,
    LucideShield, 
    LucideFileText, 
    LucideAlertTriangle, 
    LucideScale, 
    LucideExternalLink, 
    LucideMail, 
    LucideClock, 
    LucideCheckCircle2,
    LucideGlobe
  ],
  templateUrl: './terms.component.html',
  styleUrl: './terms.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TermsComponent implements OnInit {
  private readonly seoService = inject(SeoService);

  readonly lastUpdated = 'November 1, 2026';

  ngOnInit(): void {
    this.seoService.update({
      title: 'Terms of Service & DMCA Disclaimer',
      description: 'Review the Movies Center (moviescenter.site) Terms of Service, non-hosting streaming disclaimer, DMCA copyright compliance, and acceptable user conduct policies.',
      keywords: 'moviescenter terms, terms of service, dmca disclaimer, non-hosting policy, copyright compliance, moviescenter.site',
      url: 'https://moviescenter.site/terms'
    });
  }
}
