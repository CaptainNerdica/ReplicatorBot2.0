import requests
import json
import sys
import io

with open("../src/ReplicatorBot/secrets.json") as f:
    d = f.read()

secrets = json.loads(d)
token = secrets["Token"]
app_id = secrets["AppId"]

input_file = sys.argv[1]
url = f"https://discord.com/api/v8/applications/{app_id}/commands"

headers = {"Authorization": f"Bot {token}", "Content-Type": "application/json"}

f = open(input_file, "r")
json = f.read()
f.close()

r = requests.post(url, headers=headers, data=json)
response = r.json()
print(r.status_code)
print(response)
print(r.raise_for_status())

id = response["id"]
name = response["name"]
filename = f"command-{name}-{id}.json"
path = secrets["CommandGenOutputPath"]
commandfile = open(path + filename, "w")
commandfile.write(r.text)
commandfile.close()
