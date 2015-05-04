using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonarSource.TfsAnnotate
{
    class TfsCache : IDisposable
    {
        private readonly TfsClientCredentials credentials;
        private readonly IDictionary<Uri, TfsTeamProjectCollection> teamCollectionCache = new Dictionary<Uri, TfsTeamProjectCollection>();
        private readonly IDictionary<Tuple<Uri, string>, string> emailCache = new Dictionary<Tuple<Uri, string>, string>();

        public TfsCache(TfsClientCredentials credentials)
        {
            this.credentials = credentials;
        }

        private TfsTeamProjectCollection GetTeamProjectCollection(Uri serverUri)
        {
            TfsTeamProjectCollection result;
            if (!teamCollectionCache.TryGetValue(serverUri, out result))
            {
                result = new TfsTeamProjectCollection(serverUri, credentials);
                teamCollectionCache[serverUri] = result;
            }

            return result;
        }

        public VersionControlServer GetVersionControlServer(Uri serverUri)
        {
            return GetTeamProjectCollection(serverUri).GetService<VersionControlServer>();
        }

        private IIdentityManagementService GetIdentityMangementService(Uri serverUri)
        {
            return GetTeamProjectCollection(serverUri).GetService<IIdentityManagementService>();
        }

        public string GetEmailOrAccountName(Uri serverUri, string accountName)
        {
            if (accountName.Contains('@'))
            {
                // Visual Studio Online accounts are already email addresses
                return accountName;
            }

            var key = Tuple.Create(serverUri, accountName);
            string result;
            if (!emailCache.TryGetValue(key, out result))
            {
                var identity = GetIdentityMangementService(serverUri)
                    .ReadIdentity(IdentitySearchFactor.AccountName, accountName, MembershipQuery.None, ReadIdentityOptions.ExtendedProperties | ReadIdentityOptions.IncludeReadFromSource);

                if (identity == null)
                {
                    result = accountName;
                }
                else
                {
                    // ConfirmedNotificationAddress is set on the TFS profile itself
                    result = identity.GetAttribute("ConfirmedNotificationAddress", accountName);
                    if (!result.Contains('@'))
                    {
                        // Mail is supposedly fethed from AD
                        result = identity.GetAttribute("Mail", accountName);
                        if (!result.Contains('@'))
                        {
                            // Codeplex might return non-valid email addresses
                            result = accountName;
                        }
                    }
                }

                emailCache[key] = result;
            }

            return result;
        }

        public void Dispose()
        {
            foreach (var teamCollection in teamCollectionCache.Values)
            {
                teamCollection.Dispose();
            }
        }
    }
}
