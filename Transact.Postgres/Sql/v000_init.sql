-- Database: transaction

-- DROP DATABASE transaction;

CREATE DATABASE transaction
  WITH OWNER = transact
       ENCODING = 'UTF8'
       TABLESPACE = pg_default
       LC_COLLATE = ''
       LC_CTYPE = ''
       CONNECTION LIMIT = -1;

-- Schema: tr

-- DROP SCHEMA tr;

CREATE SCHEMA tr
  AUTHORIZATION transact;


-- Table: tr."Transactions"

-- DROP TABLE tr."Transactions";

CREATE TABLE tr."Transactions"
(
  "Id" uuid NOT NULL,
  "Revision" integer NOT NULL,
  "Created" timestamp without time zone NOT NULL DEFAULT timezone('utc'::text, now()),
  "Expires" timestamp without time zone,
  "Expired" timestamp without time zone,
  "Payload" jsonb,
  "Script" text,
  "ParentId" uuid,
  "ParentRevision" integer,
  "State" integer NOT NULL DEFAULT 0,
  "Handler" character varying(50),
  "Head" boolean NOT NULL DEFAULT true,
  CONSTRAINT "Transaction_pkey" PRIMARY KEY ("Id", "Revision")
)
WITH (
  OIDS=FALSE
);
ALTER TABLE tr."Transactions"
  OWNER TO postgres;
GRANT ALL ON TABLE tr."Transactions" TO postgres;
GRANT ALL ON TABLE tr."Transactions" TO public;

-- Index: tr.idx_transactions_created

-- DROP INDEX tr.idx_transactions_created;

CREATE INDEX idx_transactions_created
  ON tr."Transactions"
  USING btree
  ("Created");

-- Index: tr.idx_transactions_expired

-- DROP INDEX tr.idx_transactions_expired;

CREATE INDEX idx_transactions_expired
  ON tr."Transactions"
  USING btree
  ("Expired");

-- Index: tr.idx_transactions_expires

-- DROP INDEX tr.idx_transactions_expires;

CREATE INDEX idx_transactions_expires
  ON tr."Transactions"
  USING btree
  ("Expires");

-- Index: tr.idx_transactions_expires_expired

-- DROP INDEX tr.idx_transactions_expires_expired;

CREATE INDEX idx_transactions_expires_expired
  ON tr."Transactions"
  USING btree
  ("Expired", "Expires");

-- Index: tr.idx_transactions_id_head

-- DROP INDEX tr.idx_transactions_id_head;

CREATE INDEX idx_transactions_id_head
  ON tr."Transactions"
  USING btree
  ("Id", "Head");

-- Index: tr.idx_transactions_parent

-- DROP INDEX tr.idx_transactions_parent;

CREATE INDEX idx_transactions_parent
  ON tr."Transactions"
  USING btree
  ("ParentId");

-- Index: tr.idx_transactions_parent_revision

-- DROP INDEX tr.idx_transactions_parent_revision;

CREATE INDEX idx_transactions_parent_revision
  ON tr."Transactions"
  USING btree
  ("ParentId", "ParentRevision");

-- Index: tr.idx_transactions_revision

-- DROP INDEX tr.idx_transactions_revision;

CREATE INDEX idx_transactions_revision
  ON tr."Transactions"
  USING btree
  ("Revision");

-- View: tr."TransactionHead"

-- DROP VIEW tr."TransactionHead";

CREATE OR REPLACE VIEW tr."TransactionHead" AS 
 SELECT tr1."Id",
    tr1."Revision",
    tr1."Created",
    tr1."Expires",
    tr1."Expired",
    tr1."Payload",
    tr1."Script",
    tr1."ParentId",
    tr1."ParentRevision",
    tr1."State",
    tr1."Handler"
   FROM tr."Transactions" tr1
  WHERE tr1."Head" = true;

ALTER TABLE tr."TransactionHead"
  OWNER TO postgres;
GRANT ALL ON TABLE tr."TransactionHead" TO postgres;
GRANT SELECT ON TABLE tr."TransactionHead" TO transact;
