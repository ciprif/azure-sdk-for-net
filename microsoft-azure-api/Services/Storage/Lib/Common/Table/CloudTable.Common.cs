﻿// -----------------------------------------------------------------------------------------
// <copyright file="CloudTable.Common.cs" company="Microsoft">
//    Copyright 2012 Microsoft Corporation
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
// -----------------------------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Storage.Table
{
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Auth;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;

    /// <summary>
    /// Represents a Windows Azure table.
    /// </summary>
    public sealed partial class CloudTable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloudTable"/> class.
        /// </summary>
        /// <param name="tableAddress">The absolute URI to the table.</param>
        public CloudTable(Uri tableAddress)
            : this(tableAddress, null /* credentials */)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudTable"/> class.
        /// </summary>
        /// <param name="tableAbsoluteUri">The absolute URI to the table.</param>
        /// <param name="credentials">The account credentials.</param>
        public CloudTable(Uri tableAbsoluteUri, StorageCredentials credentials)
        {
            this.ParseQueryAndVerify(tableAbsoluteUri, credentials);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudTable"/> class.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <param name="client">The client.</param>
        internal CloudTable(string tableName, CloudTableClient client)
        {
            CommonUtility.AssertNotNull("tableName", tableName);
            CommonUtility.AssertNotNull("client", client);
            this.Name = tableName;
            this.Uri = NavigationHelper.AppendPathToUri(client.BaseUri, tableName);
            this.ServiceClient = client;
        }

        /// <summary>
        /// Gets the <see cref="CloudTableClient"/> object that represents the Table service.
        /// </summary>
        /// <value>A client object that specifies the Table service endpoint.</value>
        public CloudTableClient ServiceClient { get; private set; }

        /// <summary>
        /// Gets the table name.
        /// </summary>
        /// <value>The table name.</value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the URI that identifies the table.
        /// </summary>
        /// <value>The address of the table.</value>
        public Uri Uri { get; private set; }

        /// <summary>
        /// Returns a shared access signature for the table.
        /// </summary>
        /// <param name="policy">The access policy for the shared access signature.</param>
        /// <param name="accessPolicyIdentifier">An access policy identifier.</param>
        /// <param name="startPartitionKey">The start partition key, or null.</param>
        /// <param name="startRowKey">The start row key, or null.</param>
        /// <param name="endPartitionKey">The end partition key, or null.</param>
        /// <param name="endRowKey">The end row key, or null.</param>
        /// <returns>A shared access signature, as a URI query string.</returns>
        /// <remarks>The query string returned includes the leading question mark.</remarks>
        /// <exception cref="InvalidOperationException">Thrown if the current credentials don't support creating a shared access signature.</exception>
        public string GetSharedAccessSignature(
            SharedAccessTablePolicy policy,
            string accessPolicyIdentifier,
            string startPartitionKey,
            string startRowKey,
            string endPartitionKey,
            string endRowKey)
        {
            if (!this.ServiceClient.Credentials.IsSharedKey)
            {
                string errorMessage = string.Format(CultureInfo.CurrentCulture, SR.CannotCreateSASWithoutAccountKey);
                throw new InvalidOperationException(errorMessage);
            }

            string resourceName = this.GetCanonicalName();
            StorageAccountKey accountKey = this.ServiceClient.Credentials.Key;

            string signature = SharedAccessSignatureHelper.GetSharedAccessSignatureHashImpl(
                policy,
                accessPolicyIdentifier,
                startPartitionKey,
                startRowKey,
                endPartitionKey,
                endRowKey,
                resourceName,
                accountKey.KeyValue);

            UriQueryBuilder builder = SharedAccessSignatureHelper.GetSharedAccessSignatureImpl(
                policy,
                this.Name,
                accessPolicyIdentifier,
                startPartitionKey,
                startRowKey,
                endPartitionKey,
                endRowKey,
                signature,
                accountKey.KeyName);

            return builder.ToString();
        }

        /// <summary>
        /// Returns the name of the table.
        /// </summary>
        /// <returns>The name of the table.</returns>
        public override string ToString()
        {
            return this.Name;
        }

        /// <summary>
        /// Parse URI for SAS (Shared Access Signature) information.
        /// </summary>
        /// <param name="address">The complete Uri.</param>
        /// <param name="credentials">The credentials to use.</param>
        private void ParseQueryAndVerify(Uri address, StorageCredentials credentials)
        {
            StorageCredentials parsedCredentials;
            this.Uri = NavigationHelper.ParseQueueTableQueryAndVerify(address, out parsedCredentials);

            if ((parsedCredentials != null) && (credentials != null) && !parsedCredentials.Equals(credentials))
            {
                string error = string.Format(CultureInfo.CurrentCulture, SR.MultipleCredentialsProvided);
                throw new ArgumentException(error);
            }

            this.ServiceClient = new CloudTableClient(NavigationHelper.GetServiceClientBaseAddress(this.Uri, null), credentials ?? parsedCredentials);
            this.Name = NavigationHelper.GetTableNameFromUri(this.Uri, this.ServiceClient.UsePathStyleUris);
        }

        /// <summary>
        /// Gets the canonical name of the table, formatted as /&lt;account-name&gt;/&lt;table-name&gt;.
        /// </summary>
        /// <returns>The canonical name of the table.</returns>
        [SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.String.ToLower", Justification = "ToLower(CultureInfo) is not present in RT and ToLowerInvariant() also violates FxCop")]
        private string GetCanonicalName()
        {
            string accountName = this.ServiceClient.Credentials.AccountName;
            string tableNameLowerCase = this.Name.ToLower();

            return string.Format(CultureInfo.InvariantCulture, "/{0}/{1}", accountName, tableNameLowerCase);
        }
    }
}
