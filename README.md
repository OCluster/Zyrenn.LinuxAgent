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
