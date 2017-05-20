create table markurion.transactions (
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
	constraint fk_transactions_parent foreign key (parentId, parentRevision) references markurion.transactions
);

create table markurion.transaction_state (
	id uuid not null,
	revision int not null,
	state bytea(65536) not null default E'',
	constraint pk_transaction_state primary key(transaction_id, revision),
	constraint fk_transaction_state_transaction foreign key (id, revision) references markurion.transactions
);

create view markurion.transactions_head as 
	select id, revision, created, expires, expired, payload, script, parentId, parentRevision, state, handler, head, error 
	from markurion.transactions tr1
	where tr1.head = true;

create unique index index_transactions_head on markurion.transactions using btree (id, head) where head = true;

create index index_transactions_fk on markurion.transactions using btree (parentId, parentRevision) where parentId is not null;

create index index_transactions_id on markurion.transactions using btree (id);

create index index_transactions_expires on markurion.transactions using btree (expires) where expires is not null;

grant select, insert on table markurion.transactions to transact;
grant select, insert on table markurion.transaction_state to transact;
grant select  on table markurion.transactions_head to transact;