SHELL := /bin/bash

ENV ?= dev
DC := docker-compose -f docker/docker-compose.yml -f docker/docker-compose.${ENV}.yml
DC_RUN := ${DC} run --rm

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

.PHONY: stop
stop:
	${DC} stop
