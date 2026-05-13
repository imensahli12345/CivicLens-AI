export interface DashboardConfig {
  showStats: boolean;
  showMap: boolean;
  showRegionFeed: boolean;
  showMesSignalements: boolean;
  showCreateSignalement: boolean;
  showFilters: boolean;
  canVote: boolean;
  canDelete: boolean;
  sidebarLinks: SidebarLink[];
}

export interface SidebarLink {
  label: string;
  icon: string;
  route: string;
  badge?: number;
}

export const DASHBOARD_CONFIGS: Record<string, DashboardConfig> = {
  CITOYEN: {
    showStats: true,
    showMap: true,
    showRegionFeed: true,
    showMesSignalements: true,
    showCreateSignalement: true,
    showFilters: false,
    canVote: true,
    canDelete: true,
    sidebarLinks: [
      { label: 'Tableau de bord', icon: 'grid', route: '/dashboard' },
      { label: 'Mes signalements', icon: 'list', route: '/dashboard/mes-signalements' },
      { label: 'Nouveau signalement', icon: 'plus-circle', route: '/dashboard/nouveau' },
      { label: 'Ma région', icon: 'map-pin', route: '/dashboard/region' },
      { label: 'Événements', icon: 'calendar', route: '/dashboard/evenements' },
      { label: 'À propos', icon: 'info', route: '/dashboard/about' },
    ]
  }
};

export function getDashboardConfig(role: string): DashboardConfig {
  return DASHBOARD_CONFIGS[role] || DASHBOARD_CONFIGS['CITOYEN'];
}
