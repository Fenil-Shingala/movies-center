export interface UserProfile {
  id: string;
  email: string;
  displayName: string;
  avatarUrl?: string | null;
  roles: string[];
}

export interface AuthResponse {
  accessToken: string;
  expiresAt: string;
  user: UserProfile;
  refreshToken?: string | null;
}

export interface RegisterRequest {
  email: string;
  password: string;
  displayName: string;
  guestData?: GuestData | null;
}

export interface LoginRequest {
  email: string;
  password: string;
  guestData?: GuestData | null;
}

export interface GuestData {
  watchlistTitleIds?: number[];
  history?: GuestHistoryItem[];
}

export interface GuestHistoryItem {
  titleId: number;
  episodeId?: number | null;
  seasonNumber?: number | null;
  episodeNumber?: number | null;
  progressSeconds: number;
  durationSeconds?: number | null;
  watchedAt?: string | null;
}

export interface ImportGuestDataRequest {
  watchlistTitleIds?: number[];
  history?: GuestHistoryItem[];
}

export interface GuestMergeResult {
  watchlistCount: number;
  historyCount: number;
  message: string;
}
