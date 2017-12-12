// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Scripting
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public class NamespaceLookup
    {
        private readonly Dictionary<string, ImportedNamespace> importedNamespaces;

        public void RegisterType(Type type)
        {
            this.RegisterType(type, type.Name);
        }

        public void RegisterType(Type type, string typeAlias)
        {
            if (!this.importedNamespaces.TryGetValue(type.Namespace, out ImportedNamespace ns))
            {
                ns = new ImportedNamespace(type.Namespace);
                this.importedNamespaces.Add(type.Namespace, ns);
            }

            ns.RegisterType(type);
        }

        public NamespaceLookup()
        {
            this.importedNamespaces = new Dictionary<string, ImportedNamespace>();
        }

        public ImportedNamespace this[[NotNull] string @namespace]
        {
            get
            {
                this.importedNamespaces.TryGetValue(@namespace ?? throw new ArgumentNullException(nameof(@namespace)), out ImportedNamespace result);
                return result;
            }
        }

        public bool TryGetNamespace(string @namespace, out ImportedNamespace result)
        {
            return this.importedNamespaces.TryGetValue(@namespace, out result);
        }

        [return: CanBeNull]
        public Type GetType([NotNull] string @namespace, [NotNull] string alias)
        {
            return this[@namespace ?? throw new ArgumentNullException(nameof(@namespace))]?[alias ?? throw new ArgumentNullException(nameof(alias))];
        }

        public int Count => this.importedNamespaces.Count;
    }

    public class ImportedNamespace : IEnumerable<KeyValuePair<string, Type>>
    {
        public string Namespace { get; }

        private readonly Dictionary<string, Type> types;

        public ImportedNamespace([NotNull] string @namespace)
        {
            this.Namespace = @namespace ?? throw new ArgumentNullException(nameof(@namespace));
            this.types = new Dictionary<string, Type>();
        }

        [return: CanBeNull]
        public Type GetType([NotNull] string typeAlias)
        {
            return this.types[typeAlias ?? throw new ArgumentNullException(nameof(typeAlias))];
        }

        public void RegisterType(Type type)
        {
            this.types.Add(type.Name, type);
        }

        public void RegisterType(Type type, string typeAlias)
        {
            this.types.Add(typeAlias, type);
        }

        public int Count => this.types.Count;

        public Type this[[NotNull] string typeAlias]
        {
            [return: CanBeNull]
            get
            {
                this.types.TryGetValue(typeAlias ?? throw new ArgumentNullException(nameof(typeAlias)), out Type result);
                return result;
            }

            internal set => this.types.Add(typeAlias ?? throw new ArgumentNullException(nameof(typeAlias)), value ?? throw new ArgumentNullException(nameof(value)));
        }

        public Type this[int index] => this.types.Values.ElementAt(index);

        public IEnumerator<KeyValuePair<string, Type>> GetEnumerator()
        {
            return this.types.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
