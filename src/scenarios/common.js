import http from 'k6/http';
import { check, sleep } from 'k6';
import exec from 'k6/execution';

const context = JSON.parse(open(__ENV.LAB6_CONTEXT || '../results/load_context.json'));
const errorRate = Number(__ENV.ERROR_RATE || 0);

export function runJourney() {
    if (!__ENV.LAB6_TOKEN) {
        throw new Error('LAB6_TOKEN is required');
    }
    for (let i = 0; i < 3; i++) {
        const traceparent = i === 0 && exec.scenario.iterationInTest === 0
            ? __ENV.LAB6_TRACEPARENT
            : undefined;
        executeUserJourney(__ENV.LAB6_TOKEN, errorRate, traceparent);
    }
}

function executeUserJourney(token, errorRate, traceparent) {
    const isErrorIteration = Math.random() < errorRate;

    const headers = {
        'Content-Type': 'application/json',
        'Authorization': isErrorIteration ? 'Bearer INVALID_EXPIRED_TOKEN' : `Bearer ${token}`
    };

    const systemsRes = http.get(`${context.apiUrl}/api/v2/systems?teamId=${context.teamId}`, {
        headers: headers,
        tags: { name: 'GET /systems' }
    });

    if (!isErrorIteration) {
        check(systemsRes, { 'get systems 200': (r) => r.status === 200 });
    }

    sleep(Math.random() * 0.5 + 0.3);

    const targetSystemId = isErrorIteration
        ? '00000000-0000-0000-0000-000000000000'
        : context.systemId;

    const compRes = http.get(`${context.apiUrl}/api/v2/systems/${targetSystemId}/components`, {
        headers: headers,
        tags: { name: 'GET /systems/{id}/components' }
    });

    if (!isErrorIteration) {
        check(compRes, { 'get components 200': (r) => r.status === 200 });
    }

    sleep(Math.random() * 0.3 + 0.2);

    const targetCompId = context.components[Math.floor(Math.random() * context.components.length)];

    const analysisPayload = JSON.stringify({
        type: 'CascadingFailure',
        componentId: isErrorIteration ? '00000000-0000-0000-0000-000000000000' : targetCompId
    });

    const analysisRes = http.post(`${context.apiUrl}/api/v2/analysis`, analysisPayload, {
        headers: traceparent ? { ...headers, traceparent } : headers,
        tags: { name: 'POST /analysis' }
    });

    if (!isErrorIteration) {
        check(analysisRes, { 'analysis 200': (r) => r.status === 200 });
    }

    sleep(Math.random() * 0.5 + 0.5);
}
