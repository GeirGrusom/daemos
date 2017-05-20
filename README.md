# Daemos

## Introduction

Daemos is a transaction engine for handling time and action based operations. Basically each transaction may have a script and / or an expiration time. When the expiration
time is hit the script will get executed. The script may produce a new transaction to be executed in the future.

## Usage

Run the console application using `dotnet`. It requires PostgreSQL server to run properly although an in-memory options is available.

### Command line arguments

--database-type (-d) -- Selects the database provider. Can be either postgresql or memory.
--connection-string (-c) -- Database provider connection string.
--port (-p) -- Specifies HTTP listening port