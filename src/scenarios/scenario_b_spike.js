export { runJourney as default } from './common.js';

export const options = {
    scenarios: {
        spike_test: {
            executor: 'ramping-vus',
            startVUs: 2,
            stages: [
                { duration: '10s', target: 15 },
                { duration: '10s', target: 30 },
                { duration: '10s', target: 100 },
                { duration: '290s', target: 100 },
                { duration: '1s', target: 150 },
                { duration: '9s', target: 150 },
                { duration: '10s', target: 100 },
                { duration: '290s', target: 100 },
                { duration: '10s', target: 0 },
            ],
        },
    },
};
