using System.Net;
using System.Text.Json;
using System.Web;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Domain.Entities.Base;
using Domain.Enums;
using Infrastructure.Extensions;

namespace Infrastructure.Repositories.Base;

public abstract class DynamoRepository
{
    private readonly IAmazonDynamoDB _dynamoDb;

    internal DynamoRepository(IAmazonDynamoDB dynamoDb)
    {
        _dynamoDb = dynamoDb;
    }

    protected async Task<bool> SaveAsync(IEntity entity, CancellationToken cancellationToken)
    {
        var request = new PutItemRequest
        {
            TableName = GetTableName(),
            Item = entity.ToAttributeMap()
        };

        var response = await _dynamoDb.PutItemAsync(request, cancellationToken);
        return response.HttpStatusCode == HttpStatusCode.OK;
    }

    protected async Task<List<T>> GetAllAsync<T>(string pk, CancellationToken cancellationToken)
    {
        var entities = new List<T>();
        Dictionary<string, AttributeValue> lastKeyEvaluated = new();

        do
        {
            var request = new QueryRequest
            {
                TableName = GetTableName(),
                KeyConditionExpression = "pk = :pk",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":pk", new AttributeValue {S = pk}}
                },
                ExclusiveStartKey = lastKeyEvaluated
            };

            var response = await _dynamoDb.QueryAsync(request, cancellationToken);
            entities.AddRange(response.Items.Select(q => q.ToEntity<T>()));

            lastKeyEvaluated = response.LastEvaluatedKey;
        } while (lastKeyEvaluated.Count != 0);

        return entities;
    }

    protected async Task<List<T>> GetAllAsync<T>(string pk, string sk, SkOperator op, CancellationToken cancellationToken)
    {
        var entities = new List<T>();
        Dictionary<string, AttributeValue> lastKeyEvaluated = new();

        do
        {
            var request = new QueryRequest
            {
                TableName = GetTableName(),
                KeyConditionExpression = GetKeyConditionExpression(op),
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":pk", new AttributeValue {S = pk}},
                    {":sk", new AttributeValue {S = sk}}
                },
                ExclusiveStartKey = lastKeyEvaluated
            };

            var response = await _dynamoDb.QueryAsync(request, cancellationToken);
            entities.AddRange(response.Items.Select(q => q.ToEntity<T>()));

            lastKeyEvaluated = response.LastEvaluatedKey;
        } while (lastKeyEvaluated.Count != 0);

        return entities;
    }

    protected async Task<T?> GetAsync<T>(string pk, string sk, CancellationToken cancellationToken) where T : new()
    {
        var request = new GetItemRequest
        {
            TableName = GetTableName(),
            Key = new Dictionary<string, AttributeValue>
            {
                {"pk", new AttributeValue {S = pk}},
                {"sk", new AttributeValue {S = sk}}
            }
        };

        var response = await _dynamoDb.GetItemAsync(request, cancellationToken);
        if (response.HttpStatusCode != HttpStatusCode.OK || response.Item.Count == 0)
        {
            return default;
        }

        return response.Item.ToEntity<T>();
    }


    protected async Task<bool> DeleteAsync(string pk, string sk, CancellationToken cancellationToken)
    {
        var request = new DeleteItemRequest
        {
            TableName = GetTableName(),
            Key = new Dictionary<string, AttributeValue>
            {
                {"pk", new AttributeValue {S = pk}},
                {"sk", new AttributeValue {S = sk}}
            }
        };

        var response = await _dynamoDb.DeleteItemAsync(request, cancellationToken);
        return response.HttpStatusCode == HttpStatusCode.OK;
    }

    protected async Task BatchWriteAsync(List<IEntity> putItemList, List<IEntity> deleteItemList, CancellationToken cancellationToken)
    {
        if (!putItemList.Any() && !deleteItemList.Any())
        {
            return;
        }

        var writeRequests = new List<WriteRequest>();
        foreach (var entity in putItemList)
        {
            writeRequests.Add(new WriteRequest(new PutRequest(entity.ToAttributeMap())));
        }

        foreach (var entity in deleteItemList)
        {
            writeRequests.Add(new WriteRequest(new DeleteRequest(entity.ToKeyAttributeMap())));
        }

        var chunks = writeRequests.Chunk(25);
        var tableName = GetTableName();
        foreach (var chunk in chunks)
        {
            var request = new BatchWriteItemRequest
            {
                RequestItems = new Dictionary<string, List<WriteRequest>> {{tableName, chunk.ToList()}}
            };

            BatchWriteItemResponse response;
            do
            {
                response = await _dynamoDb.BatchWriteItemAsync(request, cancellationToken);
                request.RequestItems = response.UnprocessedItems;
            } while (response.UnprocessedItems.Count > 0);
        }
    }

    protected async Task<List<T>> BatchGetAsync<T>(List<T> entities, CancellationToken cancellationToken) where T : IEntity, new()
    {
        var result = new List<T>();
        if (!entities.Any())
        {
            return result;
        }

        var keysAndAttributes = new KeysAndAttributes();
        foreach (var entity in entities)
        {
            keysAndAttributes.Keys.Add(entity.ToKeyAttributeMap());
        }

        var tableName = GetTableName();

        var batchGetRequest = new BatchGetItemRequest
        {
            RequestItems = new Dictionary<string, KeysAndAttributes> {{tableName, keysAndAttributes}}
        };

        var response = await _dynamoDb.BatchGetItemAsync(batchGetRequest, cancellationToken);
        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            return result;
        }

        var responseList = response.Responses[tableName];
        foreach (var dict in responseList)
        {
            result.Add(dict.ToEntity<T>());
        }

        return result;
    }

    private static string GetKeyConditionExpression(SkOperator op)
    {
        var exp = "pk = :pk";
        switch (op)
        {
            case SkOperator.Equals:
            default:
                exp += " and sk = :sk";
                break;
            case SkOperator.LessThan:
                exp += " and sk < :sk";
                break;
            case SkOperator.LessThanOrEqualTo:
                exp += " and sk <= :sk";
                break;
            case SkOperator.GreaterThan:
                exp += " and sk > :sk";
                break;
            case SkOperator.GreaterThanOrEqualTo:
                exp += " and sk >= :sk";
                break;
            case SkOperator.Between:
                break;
            case SkOperator.BeginsWith:
                exp += " and begins_with (sk, :sk)";
                break;
            case SkOperator.EndsWith:
                exp += " and ends_with (sk, :sk)";
                break;
        }

        return exp;
    }

    protected async Task<(List<T> entities, string pageToken, long count)> GetPagedAsync<T>(string pk, string? pagedToken, int? limit, CancellationToken cancellationToken)
    {
        if (!limit.HasValue)
            limit = 100;
        var result = new List<T>();
        var exclusiveStartKey = new Dictionary<string, AttributeValue>();
        if (!string.IsNullOrEmpty(pagedToken))
        {
            try
            {
                exclusiveStartKey = JsonSerializer.Deserialize<Dictionary<string, AttributeValue>>(pagedToken);
            }
            catch
            {
                //ignored
            }
        }

        var queryRequest = new QueryRequest
        {
            TableName = GetTableName(),
            KeyConditionExpression = "pk = :v_pk",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                {":v_pk", new AttributeValue {S = pk}}
            },
            ExclusiveStartKey = exclusiveStartKey,
            ScanIndexForward = false
        };

        var response = await _dynamoDb.QueryAsync(queryRequest, cancellationToken);
        result.AddRange(response.Items.Select(item => item.ToEntity<T>()));

        var lastKeyEvaluated = JsonSerializer.Serialize(response.LastEvaluatedKey);

        return (result, HttpUtility.UrlEncode(lastKeyEvaluated), response.Count);
    }

    protected async Task<(List<T> entities, string pageToken, long count)> GetPagedAsync<T>(string pk, SkOperator op, string sk, string? pagedToken, int? limit, CancellationToken cancellationToken)
    {
        if (!limit.HasValue)
            limit = 100;
        var result = new List<T>();
        var exclusiveStartKey = new Dictionary<string, AttributeValue>();
        if (!string.IsNullOrEmpty(pagedToken))
        {
            try
            {
                exclusiveStartKey = JsonSerializer.Deserialize<Dictionary<string, AttributeValue>>(pagedToken);
            }
            catch
            {
                //ignored
            }
        }

        var queryRequest = new QueryRequest
        {
            TableName = GetTableName(),
            KeyConditionExpression = GetKeyConditionExpression(op),
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                {":pk", new AttributeValue {S = pk}},
                {":sk", new AttributeValue {S = sk}}
            },
            ExclusiveStartKey = exclusiveStartKey,
            ScanIndexForward = false
        };

        var response = await _dynamoDb.QueryAsync(queryRequest, cancellationToken);
        result.AddRange(response.Items.Select(item => item.ToEntity<T>()));

        var lastKeyEvaluated = JsonSerializer.Serialize(response.LastEvaluatedKey);

        return (result, HttpUtility.UrlEncode(lastKeyEvaluated), response.Count);
    }


    protected abstract string GetTableName();
}