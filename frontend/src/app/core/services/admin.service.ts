import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/media.models';
import { 
  AdminStatsOverview, 
  AdminTitleListItem, 
  AdminSource, 
  CreateAdminSourceRequest, 
  UpdateAdminSourceRequest, 
  AdminReport, 
  TmdbSearchResultItem, 
  TmdbSyncStatus, 
  TmdbSyncResult,
  UploadUrlResponse,
  UploadStatus
} from '../models/admin.models';

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private readonly http = inject(HttpClient);
  private readonly adminApi = `${environment.apiUrl}/v1/admin`;

  getStatsOverview(): Observable<AdminStatsOverview> {
    return this.http.get<AdminStatsOverview>(`${this.adminApi}/stats`);
  }

  getTitles(q?: string, type?: string, page: number = 1, pageSize: number = 20): Observable<PagedResult<AdminTitleListItem>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (q && q.trim()) params = params.set('q', q.trim());
    if (type && type !== 'all') params = params.set('type', type);

    return this.http.get<PagedResult<AdminTitleListItem>>(`${this.adminApi}/titles`, { params });
  }

  toggleTitleFeatured(id: number): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.adminApi}/titles/${id}/toggle-featured`, {});
  }

  toggleTitleActive(id: number): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.adminApi}/titles/${id}/toggle-active`, {});
  }

  searchTmdb(q: string, type?: string): Observable<TmdbSearchResultItem[]> {
    let params = new HttpParams().set('q', q.trim());
    if (type && type !== 'all') params = params.set('type', type);

    return this.http.get<TmdbSearchResultItem[]>(`${this.adminApi}/tmdb/search`, { params });
  }

  importTmdbTitle(tmdbId: number, type: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.adminApi}/tmdb/import`, { tmdbId, type });
  }

  triggerSync(): Observable<TmdbSyncResult> {
    return this.http.post<TmdbSyncResult>(`${this.adminApi}/tmdb/sync`, {});
  }

  getSyncStatus(): Observable<TmdbSyncStatus> {
    return this.http.get<TmdbSyncStatus>(`${this.adminApi}/tmdb/sync/status`);
  }

  getSources(titleId?: number, status?: string, page: number = 1, pageSize: number = 20): Observable<PagedResult<AdminSource>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (titleId && titleId > 0) params = params.set('titleId', titleId.toString());
    if (status && status !== 'all') params = params.set('status', status);

    return this.http.get<PagedResult<AdminSource>>(`${this.adminApi}/sources`, { params });
  }

  createSource(req: CreateAdminSourceRequest): Observable<AdminSource> {
    return this.http.post<AdminSource>(`${this.adminApi}/sources`, req);
  }

  updateSource(id: number, req: UpdateAdminSourceRequest): Observable<AdminSource> {
    return this.http.put<AdminSource>(`${this.adminApi}/sources/${id}`, req);
  }

  deleteSource(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.adminApi}/sources/${id}`);
  }

  getReports(status?: string, page: number = 1, pageSize: number = 20): Observable<PagedResult<AdminReport>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (status && status !== 'all') params = params.set('status', status);

    return this.http.get<PagedResult<AdminReport>>(`${this.adminApi}/reports`, { params });
  }

  updateReportStatus(id: number, status: string): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.adminApi}/reports/${id}`, { status });
  }

  createUploadUrl(fileName: string = 'video.mp4', title: string = 'Uploaded Video'): Observable<UploadUrlResponse> {
    let params = new HttpParams()
      .set('fileName', fileName)
      .set('title', title);

    return this.http.post<UploadUrlResponse>(`${this.adminApi}/uploads/create-url`, null, { params });
  }

  getUploadStatus(assetId: string): Observable<UploadStatus> {
    return this.http.get<UploadStatus>(`${this.adminApi}/uploads/${assetId}/status`);
  }
}
