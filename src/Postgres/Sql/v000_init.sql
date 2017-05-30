create schema daemos;

create table daemos.transactions (
	id uuid not null,
	revision int not null,
	created timestamp without time zone not null,
	expires timestamp without time zone null,
	expired timestamp without time zone null,
	payload jsonb null,
	script text null,
	parentId uuid null,
	parentRevision int null,
	state integer not null,
	handler varchar(50),
	head boolean not null default(true),
	error jsonb null,
	constraint pk_transactions primary key (id, revision),
	constraint fk_transactions_parent foreign key (parentId, parentRevision) references daemos.transactions
);

create table daemos.transaction_state (
	id uuid not null,
	revision int not null,
	state bytea not null default E'',
	constraint pk_transaction_state primary key(id, revision),
	constraint fk_transaction_state_transaction foreign key (id, revision) references daemos.transactions
);

create view daemos.transactions_head as 
	select id, revision, created, expires, expired, payload, script, parentId, parentRevision, state, handler, head, error 
	from daemos.transactions tr1
	where tr1.head = true;

create unique index index_transactions_head on daemos.transactions using btree (id, head) where head = true;

create index index_transactions_fk on daemos.transactions using btree (parentId, parentRevision) where parentId is not null;

create index index_transactions_id on daemos.transactions using btree (id);

create index index_transactions_expires on daemos.transactions using btree (expires) where expires is not null;

create role transact with login encrypted password 'qwerty12345';

grant usage on schema daemos to transact;
grant select, update, insert on table daemos.transactions to transact;
grant select, insert on table daemos.transaction_state to transact;
grant select on table daemos.transactions_head to transact;