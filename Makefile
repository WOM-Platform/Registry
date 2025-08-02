SHELL := /bin/bash

DC := docker compose -f docker-compose.yml -f docker-compose.local.yml -f docker-compose.custom.yml
DC_RUN := ${DC} run --rm
DB_GCLOUD := docker compose -f docker-compose.yml -f docker-compose.gcloud.yml -f docker-compose.custom.yml --env-file=gcloud.env

include gcloud.env
include config.env
export

.PHONY: confirmation
confirmation:
	@echo -n 'Are you sure? [y|N] ' && read ans && [ $$ans == y ]

.PHONY: cmd
cmd:
	@echo 'Docker-Compose command:'
	@echo '${DC}'

.PHONY: install install_example
install:
	${DC_RUN} database-client mysql -h database -u ${MYSQL_USER} -p${MYSQL_PASSWORD} ${MYSQL_DATABASE} < sql/database-create.sql

install_example:
	${DC_RUN} database-client mysql -h database -u ${MYSQL_USER} -p${MYSQL_PASSWORD} ${MYSQL_DATABASE} < sql/database-example.sql

.PHONY: mysql
mysql: up
	${DC_RUN} database-client mysql -h database -u ${MYSQL_USER} -p${MYSQL_PASSWORD} ${MYSQL_DATABASE}

.PHONY: mysqlcmd
mysqlcmd:
	@echo 'MySQL client command:'
	@echo '${DC_RUN} database-client mysql -h database -u ${MYSQL_USER} -p${MYSQL_PASSWORD} ${MYSQL_DATABASE}'

.PHONY: dump
dump:
	${DC_RUN} database-client mysqldump -h database -u ${MYSQL_USER} -p${MYSQL_PASSWORD} ${MYSQL_DATABASE} > dump.sql
	@echo 'Database exported to dump.sql.'

.PHONY: import-db
import-db: confirmation
	test -f dump.sql
	@echo 'Replacing database with contents from file dump.sql...'
	${DC_RUN} database-client mysqldump -h database -u ${MYSQL_USER} -p${MYSQL_PASSWORD} ${MYSQL_DATABASE} > previous_dump.sql
	${DC_RUN} database-client mysql -h database -u ${MYSQL_USER} -p${MYSQL_PASSWORD} ${MYSQL_DATABASE} < dump.sql
	@echo 'Database replaced. Previous contents in previous_dump.sql.'

.PHONY: phpmyadmin
phpmyadmin:
	@echo 'Running phpMyAdmin, terminate with CTRL+C...'
	${DC} up database-manager

.PHONY: mondodump mongoimport
mongodump:
	@echo 'Dumping MongoDB to mongo.zip...'
	${DC} up -d mongo
	docker exec $(shell ${DC} ps -q mongo) mongodump --uri 'mongodb://${MONGO_INITDB_ROOT_USERNAME}:${MONGO_INITDB_ROOT_PASSWORD}@mongo' --archive --gzip > mongo.zip

mongoimport: confirmation
	test -f mongo.zip
	@echo 'Replacing database with contents from file mongo.zip...'
	${DC} up -d mongo
	cat mongo.zip | docker exec --interactive $(shell ${DC} ps -q mongo) mongorestore --uri 'mongodb://${MONGO_INITDB_ROOT_USERNAME}:${MONGO_INITDB_ROOT_PASSWORD}@mongo' --authenticationDatabase admin --archive --gzip --drop --preserveUUID
	@echo 'Import completed.'

.PHONY: up
up:
	${DC} up -d api well-known
	${DC} ps
	@echo
	@echo 'WOM registry service is now up'
	@echo

.PHONY: ps
ps:
	${DC} ps

.PHONY: rs
rs:
	${DC} restart

.PHONY: rebuild
rebuild:
	${DC} build api
	${DC} rm -sf api
	${DC} up -d api well-known

.PHONY: stop
stop:
	${DC} stop

.PHONY: rm
rm:
	${DC} rm -fs

.PHONY: logs
logs:
	docker logs -f $(shell ${DC} ps -q api)

.PHONY: gcloud-deploy
gcloud-deploy:
	${DC_GCLOUD} up -d api well-known
	${DC_GCLOUD} ps
	@echo
	@echo 'WOM registry service deployed'
	@echo
