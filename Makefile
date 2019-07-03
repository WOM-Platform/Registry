SHELL := /bin/bash

DC := docker-compose -f docker/docker-compose.yml -f docker/docker-compose.custom.yml
DC_RUN := ${DC} run --rm

include docker/config.env
export

.PHONY: confirmation
confirmation:
	@echo -n 'Are you sure? [y|N] ' && read ans && [ $$ans == y ]

.PHONY: cmd
cmd:
	@echo 'Docker-Compose command:'
	@echo '${DC}'

.PHONY: create_volumes drop_volumes
create_volumes:
	docker volume create wom_registry_database
	@echo 'External volumes created'

drop_volumes: confirmation
	docker volume rm wom_registry_database
	@echo 'External volumes dropped'

.PHONY: install install_example
install:
	${DC_RUN} database-client mysql -h database -u ${MYSQL_USER} -p${MYSQL_PASSWORD} ${MYSQL_DATABASE} < sql/database-create.sql

install_example:
	${DC_RUN} database-client mysql -h database -u ${MYSQL_USER} -p${MYSQL_PASSWORD} ${MYSQL_DATABASE} < sql/database-example.sql

.PHONY: mysql
mysql: up
	${DC_RUN} database-client mysql -h database -u ${MYSQL_USER} -p${MYSQL_PASSWORD} ${MYSQL_DATABASE}

.PHONY: dump
dump:
	${DC_RUN} database-client mysqldump -h database -u ${MYSQL_USER} -p${MYSQL_PASSWORD} ${MYSQL_DATABASE} > dump.sql
	@echo 'Database exported to dump.sql.'

.PHONY: up
up:
	${DC} up -d
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
	${DC} rm -sf api
	${DC} build api
	${DC} up -d

.PHONY: stop
stop:
	${DC} stop

.PHONY: rm
rm:
	${DC} rm -fs

.PHONY: logs
logs:
	docker logs -f $(shell ${DC} ps -q api)
