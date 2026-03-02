#!/bin/sh
# Setup cron job for database backup and start crond

CRON_SCHEDULE="${BACKUP_CRON:-0 2 * * *}"

# Create cron entry with environment variables
cat > /etc/crontabs/root <<EOF
# Database backup - default: daily at 2 AM
${CRON_SCHEDULE} PGHOST=${PGHOST} PGDATABASE=${PGDATABASE} PGUSER=${PGUSER} PGPASSWORD=${PGPASSWORD} BACKUP_RETENTION_DAYS=${BACKUP_RETENTION_DAYS:-7} /backup.sh >> /var/log/backup.log 2>&1
EOF

echo "[$(date)] Cron job configured: ${CRON_SCHEDULE}"
echo "[$(date)] Starting crond..."

# Run initial backup on startup
/backup.sh

# Start cron daemon in foreground
crond -f -d 8
