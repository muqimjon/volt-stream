export interface NavItem {
  path: string;
  key: string;
  icon: string;
  permission?: string | string[];
}

export const NAV_ITEMS: NavItem[] = [
  { path: '/sale', key: 'Nav.Sales', icon: 'sale', permission: 'sales.create' },
  { path: '/supply', key: 'Web.Supply', icon: 'supply', permission: 'supplies.create' },
  { path: '/my-sales', key: 'Web.MySales', icon: 'list', permission: 'sales.view' },
  { path: '/my-supplies', key: 'Web.MySupplies', icon: 'archive', permission: 'supplies.view' },
  {
    path: '/drafts',
    key: 'Web.Drafts',
    icon: 'truck',
    permission: ['drafts.manage', 'drafts.confirm'],
  },
  { path: '/stats', key: 'Web.Stats', icon: 'stats' },
  { path: '/logs', key: 'Web.Logs', icon: 'clock', permission: 'logs.view_own' },
  { path: '/profile', key: 'Web.Profile', icon: 'user' },
];
