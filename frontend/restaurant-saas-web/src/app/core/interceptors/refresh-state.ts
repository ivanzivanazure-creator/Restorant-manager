import { BehaviorSubject } from 'rxjs';

/** Module-level (singleton) coordination state shared by every concurrent request that hits a 401,
 * so a burst of requests triggers exactly one refresh call instead of one per request. */
export const refreshState = {
  inProgress: false,
  accessToken$: new BehaviorSubject<string | null>(null),
};
