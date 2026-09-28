using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Risebird.Web.Helpers
{
    /// <summary>
    /// Safety net for dependencies that a deployment can leave out. BouncyCastle.Cryptography is only a
    /// transitive dependency of iTextSharp (used to prepare and sign the interviewer agreement PDF); a
    /// deployment that copies only the Risebird DLLs misses it, and every iTextSharp call then fails with
    /// "Could not load file or assembly 'BouncyCastle.Cryptography'". A copy is embedded in this assembly
    /// (see Risebird.Web.csproj) and loaded from there - only when .NET cannot find the file on disk.
    /// </summary>
    public static class EmbeddedAssemblyResolver
    {
        private static readonly string[] EmbeddedAssemblies = { "BouncyCastle.Cryptography" };
        private static bool _registered;

        public static void Register()
        {
            if (_registered) return;
            _registered = true;
            AssemblyLoadContext.Default.Resolving += Resolve;
        }

        private static Assembly Resolve(AssemblyLoadContext context, AssemblyName name)
        {
            if (Array.IndexOf(EmbeddedAssemblies, name.Name) < 0)
                return null;

            using var resource = typeof(EmbeddedAssemblyResolver).Assembly.GetManifestResourceStream($"EmbeddedAssemblies.{name.Name}.dll");
            if (resource == null)
                return null;
            using var ms = new MemoryStream();
            resource.CopyTo(ms);
            ms.Position = 0;
            return context.LoadFromStream(ms);
        }
    }
}
