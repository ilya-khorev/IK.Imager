using System;

namespace IK.Imager.ImageStorage.AzureFiles
{
    internal static class ArgumentHelper
    {
        /// <summary>
        /// Throws an exception if the string is empty or <c>null</c>.
        /// </summary>
        /// <param name="paramName">The name of the parameter.</param>
        /// <param name="paramValue">The value of the parameter.</param>
        /// <exception cref="T:System.ArgumentException">Thrown if value is empty.</exception>
        /// <exception cref="T:System.ArgumentNullException">Thrown if value is null.</exception>
        internal static void AssertNotNullOrEmpty(string paramName, string paramValue)
        {
            AssertNotNull(paramName, paramValue);
            if (string.IsNullOrEmpty(paramValue))
                throw new ArgumentException("The argument must not be empty string.", paramName);
        }

        /// <summary>Throws an exception if the value is null.</summary>
        /// <param name="paramName">The name of the parameter.</param>
        /// <param name="paramValue">The value of the parameter.</param>
        /// <exception cref="T:System.ArgumentNullException">Thrown if value is null.</exception>
        internal static void AssertNotNull(string paramName, object paramValue)
        {
            if (paramValue == null)
                throw new ArgumentNullException(paramName);
        }
    }
}