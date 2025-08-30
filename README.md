# Zyrenn Linux Agent

A lightweight, **data collector agent** built for modern infrastructure.  
The agent runs on Linux systems and collects system metrics, and observability signals.

## Example Payload for host metrics
```json
{
  "Name": "Something host",
  "Tag": "node 001",
  "Ips": [
    "127.0.0.1"
  ],
  "TimeStamp": "2025-08-29T21:42:37.952936Z",
  "OsType": "Linux",
  "CpuUsage": {
    "TotalUsage": 2,
    "Iowait": 5027,
    "System": 75106,
    "Idle": 13645623
  },
  "MemoryUsage": {
    "Total": 7692877824,
    "TotalUsage": 86,
    "Cache": 1725431808,
    "Used": 6684252569,
    "Free": 133064294
  },
  "DiskUsage": {
    "Total": 0,
    "Reads": 0,
    "Writes": 0
  },
  "NetworkUsage": {
    "RxBytes": 587789565,
    "TxBytes": 23555745
  }
}
```

## Example Payload for Containers
```json
{
    "Detail": {
      "Id": "00a728eef68adfaa47d97fb6399b0a7957282e674d1ecb9d92dfbdeed719e513",
      "Name": "/recursing_ramanujan",
      "Image": "nats",
      "State": {
        "Status": "exited",
        "Error": "",
        "ExitCode": 255,
        "StartedAt": "2025-05-03T16:08:29.897871+05:00",
        "FinishedAt": "2025-05-03T20:07:46.3725185+05:00",
        "Health": null
      },
      "Networks": {
        "bridge": {
          "NetworkName": "bridge",
          "MacAddress": "",
          "Gateway": "",
          "IPAddress": "",
          "IPv6Gateway": "",
          "GlobalIPv6Address": ""
        }
      }
    },
    "CpuUsage": {
      "TotalUsage": 0,
      "Iowait": 0,
      "System": 0,
      "Idle": 0
    },
    "MemoryUsage": {
      "Total": 0,
      "TotalUsage": 0,
      "Cache": 0,
      "Used": 0,
      "Free": 0
    },
    "DiskUsage": {
      "Total": 0,
      "Reads": 0,
      "Writes": 0
    },
    "NetworkUsage": {
      "RxBytes": 0,
      "TxBytes": 0
    },
    "Name": "abu usmans host",
    "Tag": "node 001",
    "Ips": [
      "127.0.1.2",
      "172.30.1.95"
    ]
  }
```

## Example Payload for Databases
```json
{
  "Timestamp": "2025-08-29T21:59:32.2388116Z",
  "Databases": [
    {
      "Name": "zyrenn",
      "Ip": "127.0.0.1/32",
      "Size": 8073699,
      "IndexCount": 3,
      "FunctionCount": 5,
      "TriggerCount": 0,
      "ViewCount": 2,
      "MaterializedViewCount": 0,
      "UserCount": 1,
      "RoleCount": 15,
      "ExtensionCount": 2,
      "ProcedureCount": 0,
      "ActiveConnectionCount": 1,
      "Status": "Online",
      "DatabaseType": "Postgres"
    },
    {
      "Name": "postgres",
      "Ip": "127.0.0.1/32",
      "Size": 7696867,
      "IndexCount": 0,
      "FunctionCount": 0,
      "TriggerCount": 0,
      "ViewCount": 0,
      "MaterializedViewCount": 0,
      "UserCount": 1,
      "RoleCount": 15,
      "ExtensionCount": 1,
      "ProcedureCount": 0,
      "ActiveConnectionCount": 1,
      "Status": "Online",
      "DatabaseType": "Postgres"
    }
  ]
}
```
