create schema trans;

set search_path to trans;

create table transactions (
	id uuid not null,
	revision int not null,
	created timestamp without time zone not null,
	expires timestamp without time zone null,
	expired timestamp without time zone null,
	payload jsonb null,
	script text null,
	parentId uuid null,
	parentRevision int null,
	status integer not null,
	head boolean not null default(true),
	error jsonb null,
	constraint pk_transactions primary key (id, revision),
	constraint fk_transactions_parent foreign key (parentId, parentRevision) references transactions
);

create table transaction_state (
	id uuid not null,
	revision int not null,
	state bytea not null default E'',
	constraint pk_transaction_state primary key(id, revision),
	constraint fk_transaction_state_transaction foreign key (id, revision) references transactions
);

create view transactions_head as 
	select id, revision, created, expires, expired, payload, script, parentId, parentRevision, status, head, error 
	from transactions tr1
	where tr1.head = true;

create unique index index_transactions_head on transactions using btree (id, head) where head = true;

create index index_transactions_fk on transactions using btree (parentId, parentRevision) where parentId is not null;

create index index_transactions_id on transactions using btree (id);

create index index_transactions_expires on transactions using btree (expires) where expires is not null;

create role transact with login encrypted password 'qwerty12345';

grant usage on schema trans to transact;
grant select, update, insert on table transactions to transact;
grant select, insert on table transaction_state to transact;
grant select on table transactions_head to transact;