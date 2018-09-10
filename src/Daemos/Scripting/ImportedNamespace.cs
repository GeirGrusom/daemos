// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Scripting
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// This class implements a imported namespace type lookup
    /// </summary>
    public class ImportedNamespace : IEnumerable<KeyValuePair<string, Type>>
    {
        private readonly Dictionary<string, Type> types;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImportedNamespace"/> class.
        /// </summary>
        /// <param name="namespace">Name of this namespace import</param>
        public ImportedNamespace([NotNull] string @namespace)
        {
            this.Namespace = @namespace ?? throw new ArgumentNullException(nameof(@namespace));
            this.types = new Dictionary<string, Type>();
        }

        /// <summary>
        /// Gets the number of types imported in this namespace
        /// </summary>
        public int Count => this.types.Count;

        /// <summary>
        /// Gets the name of this namespace import
        /// </summary>
        public string Namespace { get; }

        /// <summary>
        /// Gets a type for the specified type alias
        /// </summary>
        /// <param name="typeAlias">Type alias to look for</param>
        /// <returns>Type for the specified alias or null if it could not be found</returns>
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

        /// <summary>
        /// Gets the type at the specified index
        /// </summary>
        /// <param name="index">Index to get type from</param>
        /// <returns>Type for the index</returns>
        public Type this[int index] => this.types.Values.ElementAt(index);

        /// <summary>
        /// Gets a type in this namespace using the specified type alias
        /// </summary>
        /// <param name="typeAlias">Type alias to look for. When imported this will default to the type's name</param>
        /// <returns>Type with the specified alias or null if it could not be found</returns>
        [return: CanBeNull]
        public Type GetType([NotNull] string typeAlias)
        {
            return this.types[typeAlias ?? throw new ArgumentNullException(nameof(typeAlias))];
        }

        /// <summary>
        /// Registers a type in this namespace import. Alias will default to the type's name
        /// </summary>
        /// <param name="type">Type to register</param>
        public void RegisterType(Type type)
        {
            this.types.Add(type.Name, type);
        }

        /// <summary>
        /// Registers a type using the specified alias
        /// </summary>
        /// <param name="type">Type to register</param>
        /// <param name="typeAlias">Alias to use in place of type name</param>
        public void RegisterType(Type type, string typeAlias)
        {
            this.types.Add(typeAlias, type);
        }

        /// <inheritdoc/>
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
