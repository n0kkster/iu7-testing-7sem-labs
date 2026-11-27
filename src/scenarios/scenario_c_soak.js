export { runJourney as default } from './common.js';

export const options = {
    scenarios: {
        soak0: {
            executor: 'constant-vus',
            vus: Number(__ENV.SOAK_VUS || 100),
            duration: __ENV.SOAK_DURATION || '1m',
            gracefulStop: '0s',
        },
    },
    thresholds: {
        http_reqs: ['count>0'],
        http_req_failed: ['rate==0'],
        checks: ['rate==1'],
    },
};
