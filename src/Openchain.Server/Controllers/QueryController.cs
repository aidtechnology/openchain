// Copyright 2015 Coinprism, Inc.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Openchain.Infrastructure;
using System.Collections;
using Newtonsoft.Json.Linq;
using System.IO;
using Newtonsoft.Json;

namespace Openchain.Server.Controllers
{
    [Route("query")]
    public class QueryController : Controller
    {
        private readonly IStorageEngine storageEngine;
        private readonly ILedgerQueries store;
        private readonly ILedgerIndexes indexes;

        public QueryController(IStorageEngine storageEngine, ILedgerQueries store, ILedgerIndexes indexes)
        {
            this.storageEngine = storageEngine;
            this.store = store;
            this.indexes = indexes;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await this.storageEngine.Initialize();
            await base.OnActionExecutionAsync(context, next);
        }

        /// <summary>
        /// Gets all the records at a specific path.
        /// </summary>
        /// <param name="account">The path to query for.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        [HttpGet("account")]
        public async Task<ActionResult> GetAccount(
            [FromQuery(Name = "account")]
            string account)
        {
            IReadOnlyList<AccountStatus> accounts = await this.store.GetAccount(account);

            return Json(accounts.Select(GetAccountJson).ToArray());
        }

        /// <summary>
        /// Gets a raw transaction given its mutation hash.
        /// </summary>
        /// <param name="mutationHash">The mutation hash.</param>
        /// <param name="format">The output format ("raw" or "json").</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        [HttpGet("transaction")]
        public async Task<ActionResult> GetTransaction(
            [FromQuery(Name = "mutation_hash")]
            string mutationHash,
            [FromQuery(Name = "format")]
            string format = "raw")
        {
            ByteString parsedMutationHash;
            try
            {
                parsedMutationHash = ByteString.Parse(mutationHash);
            }
            catch (FormatException)
            {
                return BadRequest();
            }

            ByteString transaction = await this.store.GetTransaction(parsedMutationHash);

            if (transaction == null)
                return StatusCode(404);
            else
            {
                if (format == "raw")
                    return Json(new { raw = transaction.ToString() });
                else
                    return TransactionToJson(transaction);
            }
        }

        private JsonResult TransactionToJson(ByteString rawData)
        {
            Transaction transaction = MessageSerializer.DeserializeTransaction(rawData);
            Mutation mutation = MessageSerializer.DeserializeMutation(transaction.Mutation);

            return Json(new
            {
                transaction_hash = new ByteString(MessageSerializer.ComputeHash(rawData.ToByteArray())).ToString(),
                mutation_hash = new ByteString(MessageSerializer.ComputeHash(transaction.Mutation.ToByteArray())).ToString(),
                mutation = new
                {
                    @namespace = mutation.Namespace.ToString(),
                    records = mutation.Records.Select(GetRecordJson).ToArray(),
                    metadata = mutation.Metadata.ToString()
                },
                timestamp = transaction.Timestamp,
                transaction_metadata = transaction.TransactionMetadata.ToString()
            });
        }

        /// <summary>
        /// Gets all the record under a given path (includes sub-paths).
        /// </summary>
        /// <param name="account">The path to query for.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        [HttpGet("subaccounts")]
        public async Task<ActionResult> GetSubaccounts(
            [FromQuery(Name = "account")]
            string account)
        {
            LedgerPath path;
            if (!LedgerPath.TryParse(account, out path))
                return BadRequest();

            LedgerPath directory = LedgerPath.FromSegments(path.Segments.ToArray());

            IReadOnlyList<Record> accounts = await this.store.GetSubaccounts(directory.FullPath);

            return Json(accounts.Select(GetRecordJson).ToArray());
        }

        /// <summary>
        /// Gets all the mutations that have affected a given record.
        /// </summary>
        /// <param name="key">The key of the record of which mutations are being retrieved.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        [HttpGet("recordmutations")]
        public async Task<ActionResult> GetRecordMutations(
            [FromQuery(Name = "key")]
            string key)
        {
            ByteString accountKey;
            try
            {
                accountKey = ByteString.Parse(key);
            }
            catch (FormatException)
            {
                return BadRequest();
            }

            IReadOnlyList<ByteString> mutations = await this.store.GetRecordMutations(accountKey);

            return Json(mutations.Select(result => new
            {
                mutation_hash = result.ToString()
            }).ToArray());
        }

        /// <summary>
        /// Gets a specific version of a record.
        /// </summary>
        /// <param name="key">The key of the record being queried.</param>
        /// <param name="version">The version of the record being queried.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        [HttpGet("recordversion")]
        public async Task<ActionResult> GetRecordVersion(
            [FromQuery(Name = "key")]
            string key,
            [FromQuery(Name = "version")]
            string version)
        {
            ByteString parsedKey;
            ByteString parsedVersion;

            try
            {
                parsedKey = ByteString.Parse(key ?? "");
                parsedVersion = ByteString.Parse(version ?? "");
            }
            catch (FormatException)
            {
                return BadRequest();
            }

            Record record = await this.store.GetRecordVersion(parsedKey, parsedVersion);

            if (record == null)
                return NotFound();
            else
                return Json(new
                {
                    key = record.Key.ToString(),
                    value = record.Value.ToString(),
                    version = parsedVersion.ToString()
                });
        }

        /// <summary>
        /// Gets all records with a given type and name.
        /// </summary>
        /// <param name="recordName">The name of the records being queried.</param>
        /// <param name="recordType">The type of the records being queried.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        [HttpGet("recordsbyname")]
        public async Task<ActionResult> GetRecordsByName(
            [FromQuery(Name = "name")]
            string recordName,
            [FromQuery(Name = "type")]
            string recordType)
        {
            if (recordName == null)
                return BadRequest();

            RecordKey record;
            try
            {
                record = RecordKey.ParseRecord(recordType, LedgerPath.FromSegments(), recordName);
            }
            catch (ArgumentOutOfRangeException)
            {
                return BadRequest();
            }

            IReadOnlyList<Record> records = await this.indexes.GetAllRecords(record.RecordType, record.Name);

            return Json(records.Select(GetRecordJson).ToArray());
        }

        private object GetAccountJson(AccountStatus account)
        {
            return new
            {
                account = account.AccountKey.Account.FullPath,
                asset = account.AccountKey.Asset.FullPath,
                balance = account.Balance.ToString(),
                version = account.Version.ToString()
            };
        }

        private object GetRecordJson(Record record)
        {
            return new
            {
                key = record.Key.ToString(),
                value = record.Value?.ToString(),
                version = record.Version.ToString()
            };
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);

            if (context.Exception is NotSupportedException)
            {
                context.Result = StatusCode(501);
                context.ExceptionHandled = true;
            }
        }


        /// <summary>
        /// Gets all the transactions by path.
        /// </summary>
        /// <param name="path">Records path</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        [HttpGet("GetTransactionsByPath")]
        public async Task<ActionResult> GetTransactionsByPath(
            [FromQuery(Name = "path")]
            string path)
        {
            if (!LedgerPath.TryParse(path, out LedgerPath ledgerPath))
                return BadRequest();

            var directory = LedgerPath.FromSegments(ledgerPath.Segments.ToArray());
            var accounts = await this.store.GetSubaccounts(directory.FullPath);
            var keys = accounts.Where(x => RecordKey.Parse(x.Key).RecordType == RecordType.Account).Select(x => x.Key);
            var transactionsData = await this.storageEngine.GetTransactionByRecordKeys(keys, new TransactionFilter());

            var transactions = transactionsData.Select(x => new ExtTransaction(x)).ToList();

            var hashtable = new Hashtable();
            foreach (var transaction in transactions)
            {
                foreach (var record in transaction.Mutation.Records)
                {
                    var val = BitConverter.ToInt64(record.Value.Value.Reverse().ToArray(), 0);
                    hashtable.Add(transaction.MutationHash + record.Key.ToString(), val);
                }
            }

            var res = transactions.Select(x => TransactionToJsonExt(x, hashtable).Value).ToArray();
            return Json(res);
        }

        private JsonResult TransactionToJsonExt(ExtTransaction extTransaction, Hashtable hashtable)
        {
            var records = extTransaction.Mutation.Records.Select(x => RecordToJson(x, x.Version.ToString() + x.Key, hashtable)).ToArray();
            var timestamp = extTransaction.Transaction.Timestamp.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds / 1000; //convert to Unix timestamp
            return Json(new
            {
                transaction = new { timestamp = timestamp }, //TEMP //TODO: dateTime filter
                timestamp = timestamp,
                mutation = new { records },
                mutationHash = extTransaction.MutationHash,
                transactionHash = extTransaction.TransactionHash
            });
        }

        private object RecordToJson(Record record, string key, Hashtable hashtable)
        {
            var value = BitConverter.ToInt64(record.Value.Value.Reverse().ToArray(), 0);
            var prevVal = hashtable.ContainsKey(key) ? (long)hashtable[key] : 0;
            return new
            {
                key = RecordKey.Parse(record.Key),
                value = value,
                version = record.Version.ToString(),
                delta = value - prevVal
            };
        }

        //pp
        [HttpPost("GetFilteredTransactions")]
        public async Task<ActionResult> GetFilteredTransactions()
        {
            string path;
            TransactionFilter filter;
            JObject body;
            try
            {
                string bodyContent;
                using (StreamReader streamReader = new StreamReader(Request.Body))
                    bodyContent = await streamReader.ReadToEndAsync();

                body = JObject.Parse(bodyContent);
                path = body["path"].ToString();
                var jFilter = (JObject)body["filter"];
                filter = new TransactionFilter()
                {
                    StartDate = (DateTime)jFilter["start"],
                    EndDate = (DateTime)jFilter["end"]
                };
            }
            catch (JsonReaderException)
            {
                return BadRequest();
            }


            if (!LedgerPath.TryParse(path, out LedgerPath ledgerPath))
                return BadRequest();

            var directory = LedgerPath.FromSegments(ledgerPath.Segments.ToArray());
            var accounts = await this.store.GetSubaccounts(directory.FullPath);
            var keys = accounts.Where(x => RecordKey.Parse(x.Key).RecordType == RecordType.Account).Select(x => x.Key);

            var transactionsData = await this.storageEngine.GetTransactionByRecordKeys(keys, filter);
            var transactions = transactionsData.Select(x => new ExtTransaction(x)).ToList();

            var hash = new List<ByteString>();
            foreach (var transaction in transactions)
            {
                hash.AddRange(transaction.Mutation.Records.Select(x => x.Version));
            }

            var prevTransactionsData = await this.storageEngine.GetTransactionsByMutationHash(hash.Distinct());
            var prevTransactions = prevTransactionsData.Select(x => new ExtTransaction(x)).ToList();

            var hashtable = new Hashtable();
            foreach (var transaction in prevTransactions)
            {
                foreach (var record in transaction.Mutation.Records)
                {
                    var val = BitConverter.ToInt64(record.Value.Value.Reverse().ToArray(), 0);
                    hashtable.Add(transaction.MutationHash + record.Key.ToString(), val);
                }
            }

            var res = transactions.Select(x => TransactionToJsonExt(x, hashtable).Value).ToArray();
            return Json(res);
        }

        [HttpGet("SetTransactionsDate")]
        public async Task<ActionResult> SetTransactionsDate()
        {
            var count = await this.storageEngine.UpdateTransactionsDate();
            return Json(new { count = count });
        }
    }
}
