using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace ZefsjulaApi.Attributes
{
    /// <summary>
    /// Custom attribute to specify full version format (e.g., "1.0.0")
    /// This is a wrapper around the standard ApiVersion attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class ApiVersionFullAttribute : ApiVersionAttribute
    {
        /// <summary>
        /// Initializes a new instance with full version format
        /// </summary>
        /// <param name="version">Version in format "major.minor.patch" (e.g., "1.0.0")</param>
        public ApiVersionFullAttribute(string version) : base(ParseVersion(version))
        {
            FullVersion = version;
        }

        /// <summary>
        /// Gets the full version string
        /// </summary>
        public string FullVersion { get; }

        private static string ParseVersion(string fullVersion)
        {
            if (string.IsNullOrEmpty(fullVersion))
                throw new ArgumentException("Version cannot be null or empty", nameof(fullVersion));

            var parts = fullVersion.Split('.');
            if (parts.Length < 2)
                throw new ArgumentException("Version must be in format 'major.minor' or 'major.minor.patch'", nameof(fullVersion));

            // Return major.minor format for standard ApiVersion attribute
            return $"{parts[0]}.{parts[1]}";
        }
    }
}

