	create table tr.transactions (
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
		constraint fk_transactions_parent foreign key (parentId, parentRevision) references tr.transactions
	);

	create view tr.transactions_head as 
		select id, revision, created, expires, expired, payload, script, parentId, parentRevision, state, handler, head, error 
		from tr.transactions tr1
		where tr1.head = true;

create unique index index_transactions_head on tr.transactions using btree (id, head) where head = true;

create index index_transactions_fk on tr.transactions using btree (parentId, parentRevision) where parentId is not null;

create index index_transactions_id on tr.transactions using btree (id);

create index index_transactions_expires on tr.transactions using btree (expires) where expires is not null;

create view tr.transactions_head as 
	select id, revision, created, expires, expired, payload, script, parentId, parentRevision, state, handler, head, error 
	from tr.transactions tr1
	where tr1.head = true;

grant select, update, insert on table tr.transactions to transact;
grant select  on table tr.transactions_head to transact;

