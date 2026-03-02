#!/bin/sh
# Database backup script for License Management System
# Runs via cron inside the db-backup container

BACKUP_DIR="/backups"
RETENTION_DAYS=${BACKUP_RETENTION_DAYS:-7}
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="${BACKUP_DIR}/license_mgmt_${TIMESTAMP}.sql.gz"

echo "[$(date)] Starting database backup..."

# Create backup with pg_dump (compressed)
pg_dump -h "$PGHOST" -U "$PGUSER" -d "$PGDATABASE" --no-password | gzip > "$BACKUP_FILE"

if [ $? -eq 0 ]; then
    FILE_SIZE=$(du -h "$BACKUP_FILE" | cut -f1)
    echo "[$(date)] Backup completed successfully: $BACKUP_FILE ($FILE_SIZE)"
else
    echo "[$(date)] ERROR: Backup failed!"
    rm -f "$BACKUP_FILE"
    exit 1
fi

# Remove backups older than retention period
echo "[$(date)] Cleaning up backups older than $RETENTION_DAYS days..."
find "$BACKUP_DIR" -name "license_mgmt_*.sql.gz" -mtime +$RETENTION_DAYS -delete

BACKUP_COUNT=$(find "$BACKUP_DIR" -name "license_mgmt_*.sql.gz" | wc -l)
echo "[$(date)] Cleanup done. $BACKUP_COUNT backup(s) remaining."
