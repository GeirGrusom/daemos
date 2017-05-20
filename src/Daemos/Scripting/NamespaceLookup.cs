using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Daemos.Scripting
{
    public class NamespaceLookup
    {
        private readonly Dictionary<string, ImportedNamespace> _importedNamespaces;

        public void RegisterType(Type type)
        {
            RegisterType(type, type.Name);
        }

        public void RegisterType(Type type, string typeAlias)
        {
            if (!_importedNamespaces.TryGetValue(type.Namespace, out ImportedNamespace ns))
            {
                ns = new ImportedNamespace(type.Namespace);
                _importedNamespaces.Add(type.Namespace, ns);
            }
            ns.RegisterType(type);
        }

        public NamespaceLookup()
        {
            _importedNamespaces = new Dictionary<string, ImportedNamespace>();
        }

        public ImportedNamespace this[[NotNull] string @namespace]
        {
            get
            {
                _importedNamespaces.TryGetValue(@namespace ?? throw new ArgumentNullException(nameof(@namespace)), out ImportedNamespace result);
                return result;
            }
        }

        public bool TryGetNamespace(string @namespace, out ImportedNamespace result)
        {
            return _importedNamespaces.TryGetValue(@namespace, out result);
        }

        [return: CanBeNull]
        public Type GetType([NotNull] string @namespace, [NotNull] string alias)
        {
            return this[@namespace ?? throw new ArgumentNullException(nameof(@namespace))]?[alias ?? throw new ArgumentNullException(nameof(alias))];
        }

        public int Count => _importedNamespaces.Count;
    }

    public class ImportedNamespace : IEnumerable<KeyValuePair<string, Type>>
    {
        public string Namespace { get; }
        private readonly Dictionary<string, Type> _types;

        public ImportedNamespace([NotNull] string @namespace)
        {
            Namespace = @namespace ?? throw new ArgumentNullException(nameof(@namespace));
            _types = new Dictionary<string, Type>();
        }

        [return: CanBeNull]
        public Type GetType([NotNull] string typeAlias)
        {

            return _types[typeAlias ?? throw new ArgumentNullException(nameof(typeAlias))];
        }

        public void RegisterType(Type type)
        {
            _types.Add(type.Name, type);
        }

        public void RegisterType(Type type, string typeAlias)
        {
            _types.Add(typeAlias, type);
        }

        public int Count => _types.Count;

        public Type this[[NotNull] string typeAlias]
        {
            [return: CanBeNull]
            get
            {
                _types.TryGetValue(typeAlias ?? throw new ArgumentNullException(nameof(typeAlias)), out Type result);
                return result;
            }
            internal set => _types.Add(typeAlias ?? throw new ArgumentNullException(nameof(typeAlias)), value ?? throw new ArgumentNullException(nameof(value)));
        }

        public Type this[int index] => _types.Values.ElementAt(index);

        public IEnumerator<KeyValuePair<string, Type>> GetEnumerator()
        {
            return _types.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
