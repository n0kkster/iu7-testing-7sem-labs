export { runJourney as default } from './common.js';

export const options = {
    scenarios: {
        ramp_up_test: {
            executor: 'ramping-vus',
            startVUs: 2,
            stages: [
                { duration: '20s', target: 10 },
                { duration: '20s', target: 25 },
                { duration: '20s', target: 50 },
                { duration: '20s', target: 100 },
                { duration: '15s', target: 0 },
            ],
        },
    },
    thresholds: {
        http_req_failed: ['rate<0.05'],
    },
};
