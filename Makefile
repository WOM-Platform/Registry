SHELL := /bin/bash

ENV ?= dev
DC := docker-compose -f docker/docker-compose.yml -f docker/docker-compose.${ENV}.yml
DC_RUN := ${DC} run --rm

include docker/config.env
export

.PHONY: confirmation
confirmation:
	@echo -n 'Are you sure? [y|N] ' && read ans && [ $$ans == y ]

.PHONY: create_volumes drop_volumes
create_volumes:
	docker volume create wom_registry_database
	@echo 'External volumes created'

drop_volumes: confirmation
	docker volume rm wom_registry_database
	@echo 'External volumes dropped'

.PHONY: install install_example
install: create_volumes up
	${DC_RUN} database-client -h wom-database -u ${MYSQL_USER} -p${MYSQL_PASSWORD} ${MYSQL_DATABASE} < sql/database-create.sql

install_example: install
	${DC_RUN} database-client -h wom-database -u ${MYSQL_USER} -p${MYSQL_PASSWORD} ${MYSQL_DATABASE} < sql/database-example.sql

.PHONY: mysql
mysql: up
	${DC_RUN} database-client -h wom-database -u ${MYSQL_USER} -p${MYSQL_PASSWORD} ${MYSQL_DATABASE}

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
rm rmc: stop
	${DC} rm -f
