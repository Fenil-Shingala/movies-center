export interface NotificationItem {
  id: string;
  type: 'NewEpisode' | 'ContinueWatchingReminder' | 'SystemAlert' | 'NewRelease' | string;
  title: string;
  body: string;
  data: string | null;
  isRead: boolean;
  createdAt: string;
}

export interface NotificationSettings {
  newEpisodeInApp: boolean;
  newEpisodeEmail: boolean;
  newEpisodePush: boolean;
  continueWatchingInApp: boolean;
  continueWatchingEmail: boolean;
  continueWatchingPush: boolean;
}

export interface UnreadCountResponse {
  unreadCount: number;
}

export interface PagedNotificationsResponse {
  items: NotificationItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage?: boolean;
  hasNextPage?: boolean;
}
