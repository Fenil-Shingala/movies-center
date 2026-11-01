import { Injectable, inject } from '@angular/core';
import { Title, Meta } from '@angular/platform-browser';
import { DOCUMENT } from '@angular/common';

export interface SeoConfig {
  title?: string;
  description?: string;
  keywords?: string;
  image?: string;
  url?: string;
  type?: 'website' | 'video.movie' | 'video.tv_show' | 'article';
}

@Injectable({
  providedIn: 'root'
})
export class SeoService {
  private readonly titleService = inject(Title);
  private readonly meta = inject(Meta);
  private readonly doc = inject(DOCUMENT);

  private readonly defaultTitle = 'Movies Center | Watch Free Movies & TV Series Online in HD';
  private readonly defaultDescription = 'Stream over 15,000+ full HD movies and trending TV series online for free on Movies Center (moviescenter.site). Enjoy fast multi-server streaming, subtitles, and zero ads.';
  private readonly defaultKeywords = 'moviescenter.site, movies center, watch movies online, free hd movies, stream tv series, watch series online free, online movies 2026, latest movies, trending series, watch movies center';
  private readonly defaultImage = 'https://moviescenter.site/images/og-banner.png';
  private readonly defaultUrl = 'https://moviescenter.site/';

  update(config: SeoConfig): void {
    const pageTitle = config.title 
      ? `${config.title} | Movies Center` 
      : this.defaultTitle;

    const description = config.description || this.defaultDescription;
    const keywords = config.keywords || this.defaultKeywords;
    const image = config.image || this.defaultImage;
    const url = config.url || (this.doc.location ? this.doc.location.href : this.defaultUrl);
    const type = config.type || 'website';

    // 1. Page Title
    this.titleService.setTitle(pageTitle);

    // 2. Standard Meta Tags
    this.meta.updateTag({ name: 'description', content: description });
    this.meta.updateTag({ name: 'keywords', content: keywords });

    // 3. Open Graph Tags
    this.meta.updateTag({ property: 'og:title', content: pageTitle });
    this.meta.updateTag({ property: 'og:description', content: description });
    this.meta.updateTag({ property: 'og:image', content: image });
    this.meta.updateTag({ property: 'og:url', content: url });
    this.meta.updateTag({ property: 'og:type', content: type });

    // 4. Twitter Tags
    this.meta.updateTag({ name: 'twitter:title', content: pageTitle });
    this.meta.updateTag({ name: 'twitter:description', content: description });
    this.meta.updateTag({ name: 'twitter:image', content: image });

    // 5. Update Canonical link
    this.updateCanonicalUrl(url);
  }

  reset(): void {
    this.update({
      title: undefined,
      description: this.defaultDescription,
      keywords: this.defaultKeywords,
      image: this.defaultImage,
      url: this.defaultUrl,
      type: 'website'
    });
  }

  setJsonLd(schemaData: object, elementId: string = 'structured-data-detail'): void {
    try {
      let script = this.doc.getElementById(elementId) as HTMLScriptElement | null;
      if (!script) {
        script = this.doc.createElement('script');
        script.id = elementId;
        script.type = 'application/ld+json';
        this.doc.head.appendChild(script);
      }
      script.text = JSON.stringify(schemaData);
    } catch {
      // Safe fallback for SSR or environments without DOM access
    }
  }

  removeJsonLd(elementId: string = 'structured-data-detail'): void {
    try {
      const script = this.doc.getElementById(elementId);
      if (script && script.parentNode) {
        script.parentNode.removeChild(script);
      }
    } catch {
      // Ignore
    }
  }

  private updateCanonicalUrl(url: string): void {
    try {
      let link: HTMLLinkElement | null = this.doc.querySelector("link[rel='canonical']");
      if (!link) {
        link = this.doc.createElement('link');
        link.setAttribute('rel', 'canonical');
        this.doc.head.appendChild(link);
      }
      link.setAttribute('href', url);
    } catch {
      // Ignore
    }
  }
}
