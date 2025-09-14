CREATE EXTENSION IF NOT EXISTS pg_stat_statements;

SHOW log_directory;
SHOW log_filename;

    SELECT pid, wait_event_type, wait_event, query
    FROM pg_stat_activity
    WHERE wait_event IS NOT NULL;

SELECT
    blocked_locks.pid AS blocked_pid,
    blocked_activity.usename AS blocked_user,
    blocking_locks.pid AS blocking_pid,
    blocking_activity.usename AS blocking_user,
    blocked_activity.query AS blocked_query,
    blocking_activity.query AS blocking_query,
    now() - blocked_activity.query_start AS wait_duration
FROM pg_locks blocked_locks
         JOIN pg_stat_activity blocked_activity ON blocked_activity.pid = blocked_locks.pid
         JOIN pg_locks blocking_locks
              ON blocking_locks.locktype = blocked_locks.locktype
                  AND blocking_locks.database IS NOT DISTINCT FROM blocked_locks.database
                  AND blocking_locks.relation IS NOT DISTINCT FROM blocked_locks.relation
                  AND blocking_locks.page IS NOT DISTINCT FROM blocked_locks.page
                  AND blocking_locks.tuple IS NOT DISTINCT FROM blocked_locks.tuple
                  AND blocking_locks.transactionid IS NOT DISTINCT FROM blocked_locks.transactionid
                  AND blocking_locks.classid IS NOT DISTINCT FROM blocked_locks.classid
                  AND blocking_locks.objid IS NOT DISTINCT FROM blocked_locks.objid
                  AND blocking_locks.objsubid IS NOT DISTINCT FROM blocked_locks.objsubid
                  AND blocked_locks.pid != blocking_locks.pid
         JOIN pg_stat_activity blocking_activity ON blocking_activity.pid = blocking_locks.pid
WHERE NOT blocked_locks.granted;


SELECT current_database()                                              AS name,
       inet_server_addr()::varchar                                     AS ip,
    pg_database_size(current_database())                            AS size,
       -- Indexes
       (SELECT COUNT(*)
        FROM pg_class
        WHERE relkind = 'i'
          AND relnamespace IN (SELECT oid
                               FROM pg_namespace
                               WHERE nspname NOT LIKE 'pg_%'
                                 AND nspname != 'information_schema')) AS indexes,

       -- Functions
       (SELECT COUNT(*)
        FROM pg_proc
        WHERE pronamespace IN (SELECT oid
                               FROM pg_namespace
                               WHERE nspname NOT LIKE 'pg_%'
                                 AND nspname != 'information_schema')) AS functions,

       -- Triggers
       (SELECT COUNT(*)
        FROM pg_trigger
        WHERE NOT tgisinternal)                                        AS triggers,

       -- Views
       (SELECT COUNT(*)
        FROM pg_views
        WHERE schemaname NOT LIKE 'pg_%'
          AND schemaname != 'information_schema')                      AS views,

       -- Materialized Views
       (SELECT COUNT(*)
        FROM pg_matviews
        WHERE schemaname NOT LIKE 'pg_%'
          AND schemaname != 'information_schema')                      AS materialized_views,

       -- Users
       (SELECT COUNT(*)
        FROM pg_user)                                                  AS users,

       -- Roles
       (SELECT COUNT(*)
        FROM pg_roles)                                                 AS roles,

       -- Extensions
       (SELECT COUNT(*)
        FROM pg_extension)                                             AS extensions,

       -- Procedures
       (SELECT COUNT(*)
        FROM pg_proc
        WHERE prokind = 'p')                                           AS procedures,
       (SELECT CASE
                   WHEN EXISTS (SELECT 1 FROM pg_stat_activity WHERE datname = current_database()) THEN 'Online'
                   ELSE 'Offline' END)                                 AS status,
       (SELECT COUNT(*)
        FROM pg_stat_activity
        WHERE datname = current_database()
          and state = 'active')                                        as active_connections;


-- Requires pg_stat_statements extension
SELECT userid::regrole AS user_name,
    dbid::varchar   AS database_name,
    query,
       calls,
       mean_exec_time,
       --total_time,
    rows
FROM pg_stat_statements
ORDER BY mean_exec_time DESC
    LIMIT 20;


-- Blocking graph: who blocks whom
SELECT blocked_locks.pid                    AS blocked_pid,
       blocked_activity.usename             AS blocked_user,
       blocking_locks.pid                   AS blocking_pid,
       blocking_activity.usename            AS blocking_user,
       blocked_activity.query               AS blocked_query,
       blocking_activity.query              AS blocking_query,
       now() - blocked_activity.query_start AS wait_duration
FROM pg_catalog.pg_locks blocked_locks
         JOIN pg_catalog.pg_stat_activity blocked_activity ON blocked_activity.pid = blocked_locks.pid
         JOIN pg_catalog.pg_locks blocking_locks
              ON blocking_locks.locktype = blocked_locks.locktype
                  AND blocking_locks.database IS NOT DISTINCT FROM blocked_locks.database
    AND blocking_locks.relation IS NOT DISTINCT FROM blocked_locks.relation
    AND blocking_locks.page IS NOT DISTINCT FROM blocked_locks.page
    AND blocking_locks.tuple IS NOT DISTINCT FROM blocked_locks.tuple
    JOIN pg_catalog.pg_stat_activity blocking_activity ON blocking_activity.pid = blocking_locks.pid
WHERE NOT blocked_locks.granted;

-- Current blockers (active locks that may block others)
SELECT a.pid,
       a.usename,
       a.query_start,
       a.query,
       l.locktype,
       l.mode,
       l.granted
FROM pg_stat_activity a
         JOIN pg_locks l ON l.pid = a.pid
WHERE a.state = 'active'
  AND NOT l.granted;

--- identify tables with heavy sequential scans vs indexes; potential indexing opportunities.
SELECT schemaname,
       relname,
       Seq_scan,
       IDX_scan,
       (Seq_scan + IDX_scan) AS total_scans,
       CASE
           WHEN (Seq_scan + IDX_scan) > 0
               THEN (Seq_scan::numeric / NULLIF((Seq_scan + IDX_scan), 0)) * 100
           ELSE 0 END        AS seq_pct
FROM pg_stat_all_tables
WHERE schemaname NOT IN ('pg_catalog', 'information_schema')
ORDER BY total_scans DESC
    LIMIT 20;

--- quickly spot long-running active queries.
SELECT
    datname AS database_name,
    usename AS user_name,
    application_name,
    client_addr AS client_address,
    query_start,
    now() - query_start AS duration,
    state,
    query AS current_query
FROM pg_stat_activity
WHERE state = 'active'
  AND now() - query_start > INTERVAL '1 second'
ORDER BY duration DESC
    LIMIT 50;


---a single query that returns a compact health snapshot for dashboards.
--this thing or sort of this query should be used for the figma page of Database list (1.1)
SELECT
    current_database() AS name,
    inet_server_addr() AS ip,
    pg_database_size(current_database()) AS size_bytes,
    (SELECT COUNT(*) FROM pg_class WHERE relkind = 'i'
                                     AND relnamespace IN (SELECT oid FROM pg_namespace WHERE nspname NOT LIKE 'pg_%' AND nspname != 'information_schema')) AS indexes,
    (SELECT COUNT(*) FROM pg_stat_activity) AS total_connections,
    (SELECT CASE WHEN EXISTS (SELECT 1 FROM pg_stat_activity WHERE datname = current_database()) THEN 'Online' ELSE 'Offline' END) AS status;



SELECT userid::regrole        AS user_name,
    dbid::varchar          AS database_name,
    query,
       calls,
       mean_exec_time,
       calls * mean_exec_time AS total_time_ms
FROM pg_stat_statements
ORDER BY mean_exec_time DESC
    LIMIT 10;
--Query for selecting required data with one request

--extract the top three longest-running queries
SELECT
    userid :: regrole,
    dbid,
    mean_exec_time / 1000 as mean_exec_time_secs,
    max_exec_time / 1000 as max_exec_time_secs,
    min_exec_time / 1000 as min_exec_time_secs,
    stddev_exec_time,
    calls,
    query
from
    pg_stat_statements
order by
    mean_exec_time DESC limit 3;
--extract the top three longest-running queries

--I/O intensive queries
SELECT
    mean_exec_time / 1000 as mean_exec_time_secs,
    calls,
    rows,
    shared_blks_hit,
    shared_blks_read,
    shared_blks_hit /(shared_blks_hit + shared_blks_read):: NUMERIC * 100 as hit_ratio,
    (blk_read_time + blk_write_time)/calls as average_io_time_ms,
    query
FROM
    pg_stat_statements
where
    shared_blks_hit > 0
ORDER BY
    (blk_read_time + blk_write_time)/calls DESC;
--I/O intensive queries

--tables with the highest frequency of sequential scans
SELECT
    schemaname,
    relname,
    Seq_scan,
    idx_scan seq_tup_read,
    seq_tup_read / seq_scan as avg_seq_read
FROM
    pg_stat_all_tables
WHERE
    seq_scan > 0 AND schemaname not in ('pg_catalog','information_schema')
ORDER BY
    Avg_seq_read DESC LIMIT 3;
--tables with the highest frequency of sequential scans

-- infrequently accessed tables
SELECT
    schemaname,
    relname,
    seq_scan,
    idx_scan,
    (COALESCE(seq_scan, 0) + COALESCE(idx_scan, 0)) as
        total_scans_performed
FROM
    pg_stat_all_tables
WHERE
    (COALESCE(seq_scan, 0) + COALESCE(idx_scan, 0)) < 10
  AND schemaname not in ('pg_catalog', 'information_schema')
ORDER BY
    5 DESC;
-- infrequently accessed tables

--long-running queries by time 
--src: https://www.timescale.com/learn/5-ways-to-monitor-your-postgresql-database
SELECT
    datname AS database_name,
    usename AS user_name,
    application_name,
    client_addr AS client_address,
    client_hostname,
    query AS current_query,
    state,
    query_start,
    now() - query_start AS query_duration
FROM
    pg_stat_activity
WHERE
    state = 'active' AND now() - query_start > INTERVAL '0 sec'
ORDER BY
    query_start DESC;

create extension  if not exists pg_stat_monitor;

-- PostgreSQL example for slow queries
SELECT query, calls, mean_exec_time, rows
FROM pg_stat_statements
ORDER BY mean_exec_time DESC
LIMIT 10;

-- PostgreSQL memory metrics
SELECT
    sum(blks_hit)*100/GREATEST(sum(blks_hit+blks_read),1) AS cache_hit_ratio,
    count(*) filter (WHERE state = 'active') AS active_connections
FROM pg_stat_database, pg_stat_activity;

-- PostgreSQL table growth
SELECT
    relname,
    pg_size_pretty(pg_total_relation_size(relid)) AS size,
    pg_size_pretty(pg_total_relation_size(relid) - pg_relation_size(relid)) AS index_size
FROM pg_stat_user_tables
ORDER BY pg_total_relation_size(relid) DESC;

-- PostgreSQL blocking queries
SELECT blocked_locks.pid AS blocked_pid,
       blocking_locks.pid AS blocking_pid
FROM pg_catalog.pg_locks blocked_locks
         JOIN pg_catalog.pg_locks blocking_locks
              ON blocking_locks.locktype = blocked_locks.locktype
                  AND blocking_locks.DATABASE IS NOT DISTINCT FROM blocked_locks.DATABASE
                  AND blocking_locks.relation IS NOT DISTINCT FROM blocked_locks.relation
                  AND blocking_locks.page IS NOT DISTINCT FROM blocked_locks.page
                  AND blocking_locks.tuple IS NOT DISTINCT FROM blocked_locks.tuple
                  AND blocking_locks.virtualxid IS NOT DISTINCT FROM blocked_locks.virtualxid
                  AND blocking_locks.transactionid IS NOT DISTINCT FROM blocked_locks.transactionid
                  AND blocking_locks.classid IS NOT DISTINCT FROM blocked_locks.classid
                  AND blocking_locks.objid IS NOT DISTINCT FROM blocked_locks.objid
                  AND blocking_locks.objsubid IS NOT DISTINCT FROM blocked_locks.objsubid
                  AND blocking_locks.pid != blocked_locks.pid;