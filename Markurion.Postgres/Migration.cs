using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Npgsql;
using System.Linq;

namespace Markurion.Postgres
{
    public class MigrationItem
    {
        public string Name { get; }
        public Stream Script { get; }

        public ulong HashCode { get; }

        public MigrationItem(string name, Stream script, ulong hashCode)
        {
            this.Name = name;
            this.Script = script;
            this.HashCode = hashCode;
        }
    }
    public class Migration
    {
        private readonly NpgsqlConnection connection;

        public Migration(NpgsqlConnection connection)
        {
            this.connection = connection;
        }

        private string TrimStart(string input, string remove)
        {
            if (!input.StartsWith(remove))
            {
                throw new ArgumentException("Input does not start with the string that should be removed.");
            }

            return input.Substring(remove.Length);
        }

        public IEnumerable<MigrationItem> RetrieveScripts(string prefix)
        {
            var asm = GetType().GetTypeInfo().Assembly;
            var resources = asm.GetManifestResourceNames()
                .Where(x => x.StartsWith(prefix))
                .Select(x => new MigrationItem(TrimStart(x, prefix), asm.GetManifestResourceStream(x), GenerateHashCode(asm.GetManifestResourceStream(x))))
                .OrderBy(x => x.Name)
                .ToArray();

            return resources;

        }

        public void ValidateMigration()
        {
            
        }

        private ulong GenerateHashCode(Stream fileContents)
        {
            const ulong fnvOffset = 14695981039346656037;
            const ulong fnvPrime = 1099511628211;
            // We assume file contents are ASCII encoded.
            var buffer = new byte[1024];
            int readCount;
            var hash = fnvOffset;
            while (0 < (readCount = fileContents.Read(buffer, 0, buffer.Length)))
            {
                for (int i = 0; i < readCount; i++)
                {
                    if (buffer[readCount] <= ' ') // Ignore non-printing characters. 
                        continue;

                    hash = hash ^ buffer[readCount];
                    hash *= fnvPrime;
                }
            }

            return hash;
        }
    }
}
