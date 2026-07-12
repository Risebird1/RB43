using Risebird.Repository.Interface.Search;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Risebird.Repository.Search
{
    /// <summary>
    /// Resolves a listing index repository by config key (IrfListing, CandidateListing, …).
    /// Used by shared HTTP crawl/upsert endpoints so one function can serve every document type.
    /// </summary>
    public interface IListingIndexRepositoryResolver
    {
        /// <summary>Returns the repository registered for <paramref name="listingKey"/>, or null.</summary>
        IListingIndexRepository Resolve(string listingKey);

        /// <summary>All registered listing plugins.</summary>
        IReadOnlyList<IListingIndexRepository> GetAll();
    }

    /// <inheritdoc />
    public sealed class ListingIndexRepositoryResolver : IListingIndexRepositoryResolver
    {
        private readonly IReadOnlyDictionary<string, IListingIndexRepository> _byKey;

        public ListingIndexRepositoryResolver(IEnumerable<IListingIndexRepository> repositories)
        {
            _byKey = (repositories ?? Enumerable.Empty<IListingIndexRepository>())
                .ToDictionary(r => r.ListingKey, r => r, StringComparer.OrdinalIgnoreCase);
        }

        public IListingIndexRepository Resolve(string listingKey)
        {
            if (string.IsNullOrWhiteSpace(listingKey))
                return null;

            return _byKey.TryGetValue(listingKey.Trim(), out var repository) ? repository : null;
        }

        public IReadOnlyList<IListingIndexRepository> GetAll() => _byKey.Values.ToList();
    }
}
