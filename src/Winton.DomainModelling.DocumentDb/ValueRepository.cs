﻿// Copyright (c) Winton. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace Winton.DomainModelling.DocumentDb
{
    internal class ValueRepository<T> : IValueRepository<T>
        where T : IEquatable<T>
    {
        private readonly Database _database;
        private readonly IDocumentClient _documentClient;
        private readonly DocumentCollection _documentCollection;
        private readonly string _valueType;

        public ValueRepository(
            IDocumentClient documentClient,
            Database database,
            DocumentCollection documentCollection,
            string valueType)
        {
            if (documentCollection.PartitionKey.Paths.Any())
            {
                throw new NotSupportedException("Partitioned collections are not supported.");
            }

            _documentClient = documentClient;
            _database = database;
            _documentCollection = documentCollection;
            _valueType = valueType;
        }

        public async Task Delete(T value)
        {
            var document = Get(value);
            if (document != null)
            {
                await _documentClient.DeleteDocumentAsync(
                    GetUri(document.Id ?? throw new ArgumentNullException(document.Id)));
            }
        }

        public async Task Put(T value)
        {
            if (Get(value) == null)
            {
                await _documentClient.CreateDocumentAsync(GetUri(), ValueObjectDocument<T>.Create(_valueType, value));
            }
        }

        public IEnumerable<T> Query(Expression<Func<T, bool>>? predicate = null)
            => CreateValueObjectDocumentQuery()
                .Select(x => x.Value)
                .Where(predicate ?? (x => true))
                .AsEnumerable();

        private IQueryable<ValueObjectDocument<T>> CreateValueObjectDocumentQuery()
            => _documentClient
                .CreateDocumentQuery<ValueObjectDocument<T>>(GetUri())
                .Where(x => x.Type == _valueType);

        private ValueObjectDocument<T> Get(T value)
            => CreateValueObjectDocumentQuery()
                .AsEnumerable()
                .SingleOrDefault(x => x.Value.Equals(value));

        private Uri GetUri() => UriFactory.CreateDocumentCollectionUri(_database.Id, _documentCollection.Id);

        private Uri GetUri(string id) => UriFactory.CreateDocumentUri(_database.Id, _documentCollection.Id, id);
    }
}
