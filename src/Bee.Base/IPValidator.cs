using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Bee.Base
{
    /// <summary>
    /// Validates the legality of an IP address.
    /// </summary>
    /// <remarks>
    /// Supports asterisk (*) as a wildcard, e.g. 192.168.1.*
    /// Supports subnet mask notation (/) to specify IP ranges, e.g. 192.168.2.0/24
    /// </remarks>
    public class IPValidator
    {
        private readonly List<string> _whitelist;
        private readonly List<string> _blacklist;

        /// <summary>
        /// Initializes a new instance of <see cref="IPValidator"/>.
        /// </summary>
        /// <param name="whitelist">A list of whitelist IP address patterns.</param>
        /// <param name="blacklist">A list of blacklist IP address patterns.</param>
        public IPValidator(List<string> whitelist, List<string> blacklist)
        {
            _whitelist = whitelist ?? new List<string>();
            _blacklist = blacklist ?? new List<string>();
        }

        /// <summary>
        /// Gets the list of whitelist IP address patterns.
        /// </summary>
        public List<string> Whitelist
        {
            get { return _whitelist; }
        }

        /// <summary>
        /// Gets the list of blacklist IP address patterns.
        /// </summary>
        public List<string> Blacklist
        {
            get { return _blacklist; }
        }

        /// <summary>
        /// Checks whether the given IP address is allowed based on the whitelist and blacklist.
        /// </summary>
        /// <param name="ipAddress">The IP address to check.</param>
        /// <returns>True if the IP address is allowed; otherwise, false.</returns>
        public bool IsIpAllowed(string ipAddress)
        {
            // Check whether the IP address is in the blacklist
            if (IsIpBlacklisted(ipAddress))
            {
                return false;
            }

            // Check whether the IP address is in the whitelist
            return IsIpWhitelisted(ipAddress);
        }

        /// <summary>
        /// Checks whether the given IP address is in the whitelist.
        /// </summary>
        /// <param name="ipAddress">The IP address to check.</param>
        /// <returns>True if the IP address is in the whitelist; otherwise, false.</returns>
        private bool IsIpWhitelisted(string ipAddress)
        {
            foreach (string pattern in this.Whitelist)
            {
                if (IsMatch(ipAddress, pattern))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks whether the given IP address is in the blacklist.
        /// </summary>
        /// <param name="ipAddress">The IP address to check.</param>
        /// <returns>True if the IP address is in the blacklist; otherwise, false.</returns>
        private bool IsIpBlacklisted(string ipAddress)
        {
            foreach (string pattern in this.Blacklist)
            {
                if (IsMatch(ipAddress, pattern))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks whether the given IP address matches the specified pattern (supports wildcards and CIDR notation).
        /// </summary>
        /// <param name="ipAddress">The IP address to check.</param>
        /// <param name="pattern">The pattern to match against.</param>
        /// <returns>True if the IP address matches the pattern; otherwise, false.</returns>
        private bool IsMatch(string ipAddress, string pattern)
        {
            // Check whether the pattern is CIDR notation
            if (pattern.Contains('/'))
            {
                return IsInSubnet(IPAddress.Parse(ipAddress), pattern);
            }

            // Otherwise, use wildcard matching
            return IsWildcardMatch(ipAddress, pattern);
        }

        /// <summary>
        /// Checks whether the given IP address matches the wildcard pattern.
        /// </summary>
        /// <param name="ipAddress">The IP address to check.</param>
        /// <param name="pattern">The wildcard pattern to match against.</param>
        /// <returns>True if the IP address matches the wildcard pattern; otherwise, false.</returns>
        private bool IsWildcardMatch(string ipAddress, string pattern)
        {
            string[] ipParts = ipAddress.Split('.');
            string[] patternParts = pattern.Split('.');

            for (int i = 0; i < ipParts.Length; i++)
            {
                if (patternParts[i] == "*") continue;
                if (ipParts[i] != patternParts[i]) return false;
            }
            return true;
        }

        /// <summary>
        /// Checks whether the given IP address is within the specified CIDR subnet.
        /// </summary>
        /// <param name="address">The IP address to check.</param>
        /// <param name="cidr">The CIDR subnet pattern to match against.</param>
        /// <returns>True if the IP address is within the subnet; otherwise, false.</returns>
        private bool IsInSubnet(IPAddress address, string cidr)
        {
            string[] parts = cidr.Split('/');
            IPAddress ipAddress = IPAddress.Parse(parts[0]);
            int prefixLength = int.Parse(parts[1]);

            uint mask = uint.MaxValue << (32 - prefixLength);
            uint ipAddressBits = BitConverter.ToUInt32(ipAddress.GetAddressBytes().Reverse().ToArray(), 0);
            uint addressBits = BitConverter.ToUInt32(address.GetAddressBytes().Reverse().ToArray(), 0);

            return (ipAddressBits & mask) == (addressBits & mask);
        }
    }

}



