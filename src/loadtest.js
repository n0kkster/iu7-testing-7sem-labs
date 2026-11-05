import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    stages: [
        { duration: '10s', target: 30 },
        { duration: '30s', target: 30 },
        { duration: '10s', target: 0 }, 
    ],
};

const BASE_URL = 'http://localhost';

export function setup() {
    const payload = JSON.stringify({
        username: 'admin',
        password: 'admin123'
    });
    
    const params = { headers: { 'Content-Type': 'application/json' } };
    const res = http.post(`${BASE_URL}/api/v2/users/login`, payload, params);

    return { token: res.json('token') };
}

export default function (data) {
    const params = {
        headers: {
            'Authorization': `Bearer ${data.token}`,
            'Content-Type': 'application/json'
        },
    };

    let resGet = http.get(`${BASE_URL}/api/v2/teams`, params);
    if (resGet.status !== 200) {
        console.log(`GET failed with status ${resGet.status}: ${resGet.body}`);
    }
    check(resGet, { 'GET teams 200 OK': (r) => r.status === 200 });

    const postPayload = JSON.stringify({ 
        name: `TestTeam_${Math.random()}`, 
        description: "Нагрузочный тест" 
    });
    let resPost = http.post(`${BASE_URL}/api/v2/teams`, postPayload, params);
    check(resPost, { 'POST team processed': (r) => r.status === 201 || r.status === 403 || r.status === 400 });

    sleep(1);
}