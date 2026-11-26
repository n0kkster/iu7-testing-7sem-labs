#!/usr/bin/env python3
import json
import os
import sys
import urllib.error
import urllib.request
from uuid import uuid4

url = sys.argv[1]


def post(path, payload, token=None):
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    request = urllib.request.Request(f"{url}/api/v2/{path}",
                                     data=json.dumps(payload).encode(), headers=headers)
    try:
        with urllib.request.urlopen(request, timeout=15) as response:
            return json.load(response)
    except urllib.error.HTTPError as error:
        raise SystemExit(f"Ошибка подготовки данных: POST {path} → HTTP {error.code}\n"
                         f"{error.read().decode()}") from error


def login(username, password):
    return post("users/login", {"username": username, "password": password})["token"]


suffix = uuid4().hex[:10]
admin_token = login("admin", os.environ.get("LAB6_ADMIN_PASSWORD", "AdminSecret123!"))
team = post("teams", {"name": f"Perf_{suffix}", "description": "Soak test"}, admin_token)
username = f"perf_{suffix}"
email = f"{username}@loadtest.local"
password = f"Perf-{uuid4().hex}!"
invite = post(f"teams/{team['id']}/invites",
              {"email": email, "validForDays": 7, "role": 3}, admin_token)
post("users/register", {"username": username, "email": email,
                        "password": password, "inviteCode": invite["code"]})
token = login(username, password)
system = post("systems", {"name": f"Topology_{suffix}", "teamId": team["id"],
                          "description": "Soak test topology"}, token)
components = [
    post("components", {"systemId": system["id"], "type": i % 4,
                        "name": f"Service-{i}", "description": "Soak test node"}, token)["id"]
    for i in range(10)
]
for source, target in zip(components, components[1:]):
    post("links", {"sourceId": source, "targetId": target, "severity": 2, "protocol": 0}, token)
with open(sys.argv[2], "w") as context:
    json.dump({"apiUrl": url, "teamId": team["id"],
               "systemId": system["id"], "components": components}, context)
print(token)
