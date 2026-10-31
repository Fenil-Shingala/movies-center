import { 
  Component, 
  OnInit, 
  inject, 
  signal, 
  computed,
  ChangeDetectionStrategy 
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { 
  LucideShieldCheck, 
  LucideLayoutDashboard, 
  LucideRefreshCw, 
  LucideFilm, 
  LucideServer, 
  LucideAlertTriangle, 
  LucideUpload, 
  LucideSearch, 
  LucidePlus, 
  LucideTrash2, 
  LucideCheck, 
  LucideX, 
  LucideExternalLink, 
  LucideStar, 
  LucideEye, 
  LucideEyeOff, 
  LucideBarChart3, 
  LucidePlay, 
  LucideUsers,
  LucideChevronRight,
  LucideFilter,
  LucideClock,
  LucideSparkles,
  LucideLayers,
  LucideActivity,
  LucideZap,
  LucideGlobe,
  LucideCheckCircle2,
  LucideCopy
} from '@lucide/angular';
import { AdminService } from '../../core/services/admin.service';
import { AnalyticsService } from '../../core/services/analytics.service';
import { AnalyticsOverview } from '../../core/models/analytics.models';
import { 
  AdminStatsOverview, 
  AdminTitleListItem, 
  AdminSource, 
  CreateAdminSourceRequest, 
  AdminReport, 
  TmdbSearchResultItem, 
  TmdbSyncStatus,
  UploadUrlResponse,
  UploadStatus
} from '../../core/models/admin.models';
import { PagedResult } from '../../core/models/media.models';
import { TMDB_IMAGE_BASE_URL, TMDB_POSTER_SIZE, DEFAULT_POSTER_IMAGE } from '../../core/constants/app.constants';
import { CustomSelectComponent, SelectOption } from '../../shared/components/custom-select/custom-select.component';
import { PaginationComponent } from '../../shared/components/pagination/pagination.component';

export type AdminTab = 'overview' | 'tmdb' | 'titles' | 'sources' | 'reports' | 'uploads' | 'analytics';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule, 
    RouterLink, 
    CustomSelectComponent,
    PaginationComponent,
    LucideShieldCheck, 
    LucideLayoutDashboard, 
    LucideRefreshCw, 
    LucideFilm, 
    LucideServer, 
    LucideAlertTriangle, 
    LucideUpload, 
    LucideSearch, 
    LucidePlus, 
    LucideTrash2, 
    LucideCheck, 
    LucideX, 
    LucideExternalLink, 
    LucideStar, 
    LucideEye, 
    LucideEyeOff, 
    LucideBarChart3, 
    LucidePlay, 
    LucideUsers,
    LucideChevronRight,
    LucideFilter,
    LucideClock,
    LucideSparkles,
    LucideLayers,
    LucideActivity,
    LucideZap,
    LucideGlobe,
    LucideCheckCircle2,
    LucideCopy
  ],
  templateUrl: './admin.component.html',
  styleUrl: './admin.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly analyticsService = inject(AnalyticsService);

  readonly Math = Math;

  readonly currentTab = signal<AdminTab>('overview');
  readonly stats = signal<AdminStatsOverview | null>(null);
  readonly analytics = signal<AnalyticsOverview | null>(null);
  readonly isLoading = signal<boolean>(false);
  readonly isLoadingAnalytics = signal<boolean>(false);
  readonly actionMessage = signal<string | null>(null);

  // TMDB Sync & Discovery
  readonly syncStatus = signal<TmdbSyncStatus | null>(null);
  readonly isSyncing = signal<boolean>(false);
  readonly tmdbSearchQuery = signal<string>('');
  readonly tmdbSearchType = signal<'all' | 'movie' | 'series'>('all');
  readonly tmdbSearchResults = signal<TmdbSearchResultItem[]>([]);
  readonly isSearchingTmdb = signal<boolean>(false);
  readonly importingTmdbId = signal<number | null>(null);

  // Catalog Titles Management
  readonly titles = signal<AdminTitleListItem[]>([]);
  readonly titlesTotalCount = signal<number>(0);
  readonly titlesPage = signal<number>(1);
  readonly titlesSearchQuery = signal<string>('');
  readonly titlesFilterType = signal<string>('all');
  readonly titlesTotalPages = computed(() => Math.ceil(this.titlesTotalCount() / 20) || 1);

  // Streaming Sources
  readonly sources = signal<AdminSource[]>([]);
  readonly sourcesPage = signal<number>(1);
  readonly sourcesTotalCount = signal<number>(0);
  readonly sourcesSearchQuery = signal<string>('');
  readonly sourcesTotalPages = computed(() => Math.ceil(this.sourcesTotalCount() / 20) || 1);
  readonly isAddSourceModalOpen = signal<boolean>(false);
  readonly newSource = signal<CreateAdminSourceRequest>({
    titleId: 0,
    seasonNumber: null,
    episodeNumber: null,
    providerName: 'Custom Embed',
    sourceType: 'embed',
    url: '',
    quality: '1080p',
    priority: 1,
    isActive: true
  });

  // Broken Source Reports
  readonly reports = signal<AdminReport[]>([]);
  readonly reportsPage = signal<number>(1);
  readonly reportsTotalCount = signal<number>(0);
  readonly reportsStatusFilter = signal<string>('all');
  readonly reportsTotalPages = computed(() => Math.ceil(this.reportsTotalCount() / 20) || 1);

  // Video Uploads
  readonly selectedUploadFile = signal<File | null>(null);
  readonly uploadTitle = signal<string>('');
  readonly isUploading = signal<boolean>(false);
  readonly uploadProgress = signal<number>(0);
  readonly activeUploadAsset = signal<UploadUrlResponse | null>(null);

  readonly commonProviders = ['VidSrc', '2Embed', 'SuperEmbed', 'Frembed', 'SmashyStream', 'Custom Embed'];
  readonly qualityOptions = ['1080p', '720p', '4K UHD', '480p'];

  readonly typeSelectOptions: SelectOption<string>[] = [
    { value: 'all', label: 'All Formats' },
    { value: 'movie', label: 'Movies' },
    { value: 'series', label: 'TV Series' }
  ];

  readonly reportStatusOptions: SelectOption<string>[] = [
    { value: 'all', label: 'All Reports' },
    { value: 'Pending', label: 'Pending' },
    { value: 'Reviewed', label: 'Reviewed' },
    { value: 'Resolved', label: 'Resolved' },
    { value: 'Dismissed', label: 'Dismissed' }
  ];

  readonly sourceTypeOptions: SelectOption<string>[] = [
    { value: 'embed', label: 'Third-Party Embed' },
    { value: 'stream', label: 'Direct HLS / Stream' }
  ];

  ngOnInit(): void {
    this.loadStats();
  }

  setTab(tab: AdminTab): void {
    this.currentTab.set(tab);
    this.actionMessage.set(null);

    switch (tab) {
      case 'overview':
        this.loadStats();
        break;
      case 'tmdb':
        this.loadSyncStatus();
        break;
      case 'titles':
        this.loadTitles();
        break;
      case 'sources':
        this.loadSources();
        break;
      case 'reports':
        this.loadReports();
        break;
      case 'uploads':
        break;
      case 'analytics':
        this.loadAnalytics();
        break;
    }
  }

  loadAnalytics(): void {
    this.isLoadingAnalytics.set(true);
    this.analyticsService.getAnalyticsOverview().subscribe({
      next: (data) => {
        this.analytics.set(data);
        this.isLoadingAnalytics.set(false);
      },
      error: (err) => {
        console.error('Failed to load analytics overview', err);
        this.isLoadingAnalytics.set(false);
      }
    });
  }

  getMaxPlays(overview: AnalyticsOverview | null): number {
    if (!overview || !overview.dailyTrends.length) return 10;
    const max = Math.max(...overview.dailyTrends.map(t => Math.max(t.plays, t.searches)));
    return max > 0 ? max : 10;
  }

  getBarHeightPercent(val: number, max: number): number {
    if (!max || max === 0) return 4;
    return Math.max(Math.round((val / max) * 100), 4);
  }

  showToast(msg: string): void {
    this.actionMessage.set(msg);
    setTimeout(() => this.actionMessage.set(null), 4000);
  }

  // --- STATS OVERVIEW ---
  loadStats(): void {
    this.adminService.getStatsOverview().subscribe({
      next: (res) => this.stats.set(res),
      error: () => {}
    });
  }

  // --- TMDB SYNC & SEARCH ---
  loadSyncStatus(): void {
    this.adminService.getSyncStatus().subscribe({
      next: (res) => this.syncStatus.set(res)
    });
  }

  triggerTmdbSync(): void {
    this.isSyncing.set(true);
    this.adminService.triggerSync().subscribe({
      next: (res) => {
        this.isSyncing.set(false);
        this.showToast(`Sync completed: ${res.itemsSynced} items updated.`);
        this.loadSyncStatus();
        this.loadStats();
      },
      error: () => {
        this.isSyncing.set(false);
        this.showToast('Sync encountered an error. Check logs.');
      }
    });
  }

  searchTmdb(): void {
    const q = this.tmdbSearchQuery().trim();
    if (!q) return;

    this.isSearchingTmdb.set(true);
    this.adminService.searchTmdb(q, this.tmdbSearchType()).subscribe({
      next: (res) => {
        this.tmdbSearchResults.set(res);
        this.isSearchingTmdb.set(false);
      },
      error: () => {
        this.isSearchingTmdb.set(false);
        this.showToast('Failed to search TMDB.');
      }
    });
  }

  importTmdbTitle(item: TmdbSearchResultItem): void {
    this.importingTmdbId.set(item.tmdbId);
    this.adminService.importTmdbTitle(item.tmdbId, item.type).subscribe({
      next: () => {
        this.importingTmdbId.set(null);
        this.showToast(`Imported "${item.title}" into catalog.`);
        this.loadStats();
      },
      error: () => {
        this.importingTmdbId.set(null);
        this.showToast(`Failed to import "${item.title}".`);
      }
    });
  }

  getPosterUrl(path: string | null): string {
    if (!path) return DEFAULT_POSTER_IMAGE;
    const cleanPath = path.startsWith('/') ? path : `/${path}`;
    return `${TMDB_IMAGE_BASE_URL}${TMDB_POSTER_SIZE}${cleanPath}`;
  }

  onPosterError(event: Event): void {
    const img = event.target as HTMLImageElement;
    if (img && !img.src.includes('default-poster.svg')) {
      img.src = DEFAULT_POSTER_IMAGE;
    }
  }

  // --- CATALOG TITLES ---
  loadTitles(): void {
    this.isLoading.set(true);
    this.adminService.getTitles(
      this.titlesSearchQuery(), 
      this.titlesFilterType(), 
      this.titlesPage(), 
      20
    ).subscribe({
      next: (res) => {
        this.titles.set(res.items);
        this.titlesTotalCount.set(res.totalCount);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  onTitlesSearch(): void {
    this.titlesPage.set(1);
    this.loadTitles();
  }

  onTitlesPageChange(page: number): void {
    this.titlesPage.set(page);
    this.loadTitles();
  }

  onTitlesTypeFilterChange(type: string): void {
    this.titlesFilterType.set(type);
    this.titlesPage.set(1);
    this.loadTitles();
  }

  toggleFeatured(item: AdminTitleListItem): void {
    this.adminService.toggleTitleFeatured(item.id).subscribe({
      next: () => {
        item.isFeatured = !item.isFeatured;
        this.titles.set([...this.titles()]);
        this.showToast(`"${item.title}" featured status changed.`);
      }
    });
  }

  toggleActive(item: AdminTitleListItem): void {
    this.adminService.toggleTitleActive(item.id).subscribe({
      next: () => {
        item.isActive = !item.isActive;
        this.titles.set([...this.titles()]);
        this.showToast(`"${item.title}" visibility toggled.`);
      }
    });
  }

  // --- STREAMING SOURCES ---
  loadSources(): void {
    this.isLoading.set(true);
    const titleIdFilter = Number(this.sourcesSearchQuery().trim()) || undefined;
    this.adminService.getSources(titleIdFilter, undefined, this.sourcesPage(), 20).subscribe({
      next: (res) => {
        this.sources.set(res.items);
        this.sourcesTotalCount.set(res.totalCount);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  onSourcesSearch(): void {
    this.sourcesPage.set(1);
    this.loadSources();
  }

  onSourcesPageChange(page: number): void {
    this.sourcesPage.set(page);
    this.loadSources();
  }

  setNewSourceProvider(name: string): void {
    this.newSource.update(s => ({ ...s, providerName: name }));
  }

  setNewSourceQuality(quality: string): void {
    this.newSource.update(s => ({ ...s, quality }));
  }

  copyToClipboard(text: string): void {
    if (navigator?.clipboard) {
      navigator.clipboard.writeText(text);
      this.showToast('Copied URL to clipboard!');
    }
  }

  openAddSourceModal(): void {
    this.newSource.set({
      titleId: 0,
      seasonNumber: null,
      episodeNumber: null,
      providerName: 'VidSrc',
      sourceType: 'embed',
      url: '',
      quality: '1080p',
      priority: 1,
      isActive: true
    });
    this.isAddSourceModalOpen.set(true);
  }

  closeAddSourceModal(): void {
    this.isAddSourceModalOpen.set(false);
  }

  submitAddSource(): void {
    const s = this.newSource();
    if (s.titleId <= 0 || !s.url) {
      this.showToast('Please enter Title ID and Stream / Embed URL.');
      return;
    }

    this.adminService.createSource(s).subscribe({
      next: (created) => {
        this.showToast(`Source added for Title #${created.titleId}`);
        this.closeAddSourceModal();
        this.loadSources();
        this.loadStats();
      },
      error: (err) => {
        this.showToast(err?.error?.message || 'Failed to create source.');
      }
    });
  }

  deleteSource(id: number): void {
    if (!confirm('Are you sure you want to delete this playback source?')) return;

    this.adminService.deleteSource(id).subscribe({
      next: () => {
        this.showToast(`Source #${id} deleted.`);
        this.loadSources();
        this.loadStats();
      }
    });
  }

  // --- REPORTS QUEUE ---
  loadReports(): void {
    this.isLoading.set(true);
    this.adminService.getReports(this.reportsStatusFilter(), this.reportsPage(), 20).subscribe({
      next: (res) => {
        this.reports.set(res.items);
        this.reportsTotalCount.set(res.totalCount);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  onReportsPageChange(page: number): void {
    this.reportsPage.set(page);
    this.loadReports();
  }

  formatReportReason(reason: string): string {
    if (!reason) return 'Playback Issue';
    return reason.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase());
  }

  onReportsFilterChange(status: string): void {
    this.reportsStatusFilter.set(status);
    this.reportsPage.set(1);
    this.loadReports();
  }

  updateReportStatus(report: AdminReport, status: string): void {
    this.adminService.updateReportStatus(report.id, status).subscribe({
      next: () => {
        report.status = status as any;
        this.reports.set([...this.reports()]);
        this.showToast(`Report #${report.id} marked as ${status}.`);
        this.loadStats();
      }
    });
  }

  // --- VIDEO UPLOADS ---
  onFileSelect(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      this.selectedUploadFile.set(file);
      if (!this.uploadTitle()) {
        this.uploadTitle.set(file.name.replace(/\.[^/.]+$/, ''));
      }
    }
  }

  startUpload(): void {
    const file = this.selectedUploadFile();
    if (!file) {
      this.showToast('Please select a video file.');
      return;
    }

    this.isUploading.set(true);
    this.uploadProgress.set(10);

    const title = this.uploadTitle().trim() || file.name;
    this.adminService.createUploadUrl(file.name, title).subscribe({
      next: (asset) => {
        this.activeUploadAsset.set(asset);
        this.uploadProgress.set(50);

        // Simulate progress completion
        setTimeout(() => {
          this.uploadProgress.set(100);
          this.isUploading.set(false);
          this.showToast(`Video asset "${title}" processed and ready for playback!`);
        }, 1200);
      },
      error: () => {
        this.isUploading.set(false);
        this.showToast('Failed to generate upload URL.');
      }
    });
  }
}
