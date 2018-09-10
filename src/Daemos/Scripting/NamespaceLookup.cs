// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Scripting
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// This class implements type lookup logic for namespaces
    /// </summary>
    public class NamespaceLookup
    {
        private readonly Dictionary<string, ImportedNamespace> importedNamespaces;

        /// <summary>
        /// Initializes a new instance of the <see cref="NamespaceLookup"/> class.
        /// </summary>
        public NamespaceLookup()
        {
            this.importedNamespaces = new Dictionary<string, ImportedNamespace>();
        }

        /// <summary>
        /// Gets the number of imported namespaces
        /// </summary>
        public int Count => this.importedNamespaces.Count;

        /// <summary>
        /// Gets an imported namespace
        /// </summary>
        /// <param name="namespace">Namespace to retrieve lookup for</param>
        /// <returns>Imported namespace lookup</returns>
        public ImportedNamespace this[[NotNull] string @namespace]
        {
            get
            {
                this.importedNamespaces.TryGetValue(@namespace ?? throw new ArgumentNullException(nameof(@namespace)), out ImportedNamespace result);
                return result;
            }
        }

        /// <summary>
        /// Registers a type in a namespace
        /// </summary>
        /// <param name="type">Type to register</param>
        public void RegisterType(Type type)
        {
            this.RegisterType(type, type.Name);
        }

        /// <summary>
        /// Registers a type in a namespace with the specified type alias
        /// </summary>
        /// <param name="type">Type to register</param>
        /// <param name="typeAlias">Alias to use</param>
        public void RegisterType(Type type, string typeAlias)
        {
            if (!this.importedNamespaces.TryGetValue(type.Namespace, out ImportedNamespace ns))
            {
                ns = new ImportedNamespace(type.Namespace);
                this.importedNamespaces.Add(type.Namespace, ns);
            }

            ns.RegisterType(type);
        }

        /// <summary>
        /// Tries to retrieve a namespace lookup if it exists
        /// </summary>
        /// <param name="namespace">Namespace to retrieve</param>
        /// <param name="result">Retrieved namespace. This will be null if the lookup fails</param>
        /// <returns>True if lookup was successful, otherwise false</returns>
        public bool TryGetNamespace(string @namespace, out ImportedNamespace result)
        {
            return this.importedNamespaces.TryGetValue(@namespace, out result);
        }

        /// <summary>
        /// Gets a type in the specified namespace using the specified alias
        /// </summary>
        /// <param name="namespace">Namespace </param>
        /// <param name="alias">Alias to look for</param>
        /// <returns>Type in the specified namespace, or null if it could not be found.</returns>
        [return: CanBeNull]
        public Type GetType([NotNull] string @namespace, [NotNull] string alias)
        {
            return this[@namespace ?? throw new ArgumentNullException(nameof(@namespace))]?[alias ?? throw new ArgumentNullException(nameof(alias))];
        }
    }
}