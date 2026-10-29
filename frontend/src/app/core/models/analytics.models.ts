export interface AnalyticsEvent {
  eventName: string;
  dimension?: string;
  sessionId?: string;
  timestamp?: string;
}

export interface IngestEventsRequest {
  events: AnalyticsEvent[];
}

export interface DailyMetricTrend {
  date: string;
  plays: number;
  searches: number;
  signups: number;
}

export interface TopMetricItem {
  name: string;
  value: number;
  titleId?: number | null;
  hasResults?: boolean | null;
}

export interface AnalyticsOverview {
  totalPlays: number;
  totalSearches: number;
  totalEmptySearches: number;
  totalSignups: number;
  dailyTrends: DailyMetricTrend[];
  topPlayedTitles: TopMetricItem[];
  topSearchQueries: TopMetricItem[];
  topEmptySearches: TopMetricItem[];
  failingSources: TopMetricItem[];
}
