import { useCallback, useEffect, useState } from 'react';

/** Pages of the portal, in the order they appear in the navigation. */
export type PageId =
  | 'overview'
  | 'sessions'
  | 'live'
  | 'followups'
  | 'audit'
  | 'reports'
  | 'planning'
  | 'retrospective'
  | 'stories'
  | 'models'
  | 'schedule'
  | 'prompts'
  | 'team'
  | 'roster'
  | 'fleet'
  | 'teams'
  | 'onboarding';

const pages: PageId[] = [
  'overview',
  'sessions',
  'live',
  'followups',
  'audit',
  'reports',
  'planning',
  'retrospective',
  'stories',
  'models',
  'schedule',
  'prompts',
  'team',
  'roster',
  'fleet',
  'teams',
  'onboarding',
];

function read(): PageId {
  const id = location.hash.replace(/^#\/?/, '') as PageId;
  return pages.includes(id) ? id : 'overview';
}

/**
 * Hash-based routing, so every page has a link that can be shared or bookmarked without server routing.
 * @returns The current page and a function to go to another.
 */
export function useRoute(): { page: PageId; go: (page: PageId) => void } {
  const [page, setPage] = useState<PageId>(read);
  useEffect(() => {
    const onChange = () => setPage(read());
    window.addEventListener('hashchange', onChange);
    return () => window.removeEventListener('hashchange', onChange);
  }, []);
  const go = useCallback((next: PageId) => {
    location.hash = `/${next}`;
  }, []);
  return { page, go };
}
